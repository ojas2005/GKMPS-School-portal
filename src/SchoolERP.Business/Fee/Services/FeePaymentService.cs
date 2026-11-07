using System.Security.Cryptography;
using QuestPDF.Fluent;
using SchoolERP.Business.Fee.Documents;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.DataAccess.Storage;
using SchoolERP.Common;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Fee.Services;

/// <summary>
/// Every payment: appends an immutable PaymentTransaction ledger entry and bumps the fee's
/// PaidAmount in one database transaction -- the bump is guarded, so a fee can never be paid
/// beyond what it still owes -- then publishes FeePaidEvent. Receipts are QuestPDF documents
/// made on first download and kept in storage. Concessions and approved waivers go through the
/// same kind of guarded update and are written to the fee history.
/// </summary>
public class FeePaymentService : IFeePaymentService
{
    private readonly IFeePaymentRepository _payments;
    private readonly IFeeStructureRepository _structures;
    private readonly IPaymentTransactionRepository _transactions;
    private readonly IFeeAccountRepository _accounts;
    private readonly IStudentService _students;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventPublisher _events;
    private readonly TimeProvider _clock;
    private readonly ILogger<FeePaymentService> _logger;
    private readonly string _schoolName;

    private const string ContainerName = "fee-receipts";

    public FeePaymentService(
        IFeePaymentRepository payments,
        IFeeStructureRepository structures,
        IPaymentTransactionRepository transactions,
        IFeeAccountRepository accounts,
        IStudentService students,
        IBlobStorageService blobStorage,
        IEventPublisher events,
        IConfiguration configuration,
        TimeProvider clock,
        ILogger<FeePaymentService> logger)
    {
        _payments = payments;
        _structures = structures;
        _transactions = transactions;
        _accounts = accounts;
        _students = students;
        _blobStorage = blobStorage;
        _events = events;
        _clock = clock;
        _logger = logger;
        _schoolName = configuration["School:Name"] ?? "School";
    }

    private DateTime Today => SchoolClock.Today(_clock);

    public async Task<FeePaymentSummary> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        // Two ways to identify what's being paid: a specific fee directly (the normal way), or
        // an older class fee structure it's linked to (creates the row on first payment).
        FeePayment? payment;
        if (request.FeePaymentId.HasValue)
        {
            payment = await _payments.FindByIdAsync(request.FeePaymentId.Value, ct)
                ?? throw new KeyNotFoundException("Fee due not found.");
            if (payment.StudentId != request.StudentId)
                throw new InvalidOperationException("That fee belongs to a different student.");
        }
        else
        {
            if (!request.FeeStructureId.HasValue)
                throw new InvalidOperationException("Either feePaymentId or feeStructureId is required.");

            var structure = await _structures.FindByIdAsync(request.FeeStructureId.Value, ct)
                ?? throw new KeyNotFoundException("Fee structure not found.");

            payment = await _payments.FindByStudentAndStructureAsync(request.StudentId, request.FeeStructureId.Value, ct);
            if (payment is null)
            {
                payment = new FeePayment
                {
                    StudentId = request.StudentId,
                    FeeStructureId = structure.Id,
                    ClassId = structure.ClassId,
                    AcademicYear = structure.AcademicYear,
                    PeriodLabel = structure.Name.Length <= 50 ? structure.Name : structure.Name[..50],
                    DueDate = structure.DueDateUtc.Date,
                    TotalAmount = structure.Amount
                };
                await _payments.AddAsync(payment, ct);
                await _payments.SaveChangesAsync(ct);
            }
        }

        var transaction = await AppendTransactionAsync(payment, request.Amount, request.PaymentMethod, request.GatewayReference, ct);

        _logger.LogInformation("Fee payment recorded: {ReceiptNumber} amount={Amount} student={StudentId}", transaction.ReceiptNumber, request.Amount, request.StudentId);

        return ToSummary(payment);
    }

    public async Task<FeeSubmissionResult> SubmitPaymentAsync(Guid studentId, SubmitFeePaymentRequest request, CancellationToken ct = default)
    {
        // Always a brand-new charge for a custom period, paid in the same step.
        var payment = NewManualCharge(studentId, request.ClassId, request.Amount, request.PeriodLabel, request.Description, Today);
        await _payments.AddAsync(payment, ct);
        await _payments.SaveChangesAsync(ct);

        var transaction = await AppendTransactionAsync(payment, request.Amount, request.PaymentMethod, request.GatewayReference, ct);

        _logger.LogInformation("Fee submitted: {ReceiptNumber} amount={Amount} period={Period} student={StudentId}",
            transaction.ReceiptNumber, request.Amount, request.PeriodLabel, studentId);

        return new FeeSubmissionResult(ToSummary(payment), transaction.Id, transaction.ReceiptNumber);
    }

    public async Task<FeePaymentSummary> AddAdHocDueAsync(Guid studentId, AddAdHocDueRequest request, Guid? actorUserId = null, string? actorRole = null, CancellationToken ct = default)
    {
        // Nothing was paid: PaidAmount stays 0 and no transaction, receipt or event is produced.
        var payment = NewManualCharge(studentId, request.ClassId, request.Amount, request.PeriodLabel, request.Description, (request.DueDate ?? Today).Date);
        await _payments.AddAsync(payment, ct);
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = studentId, FeePaymentId = payment.Id, Kind = FeeAdjustmentKinds.ChargeAdded, Amount = request.Amount,
            Reason = request.Description ?? request.PeriodLabel, ActorUserId = actorUserId, ActorRole = actorRole
        }, ct);
        await _payments.SaveChangesAsync(ct);

        _logger.LogInformation("Charge added: amount={Amount} period={Period} student={StudentId}",
            request.Amount, request.PeriodLabel, studentId);

        return ToSummary(payment);
    }

    private static FeePayment NewManualCharge(Guid studentId, string classId, decimal amount, string periodLabel, string? description, DateTime dueDate) => new()
    {
        StudentId = studentId,
        ClassId = classId,
        Category = FeeCategories.Other,
        AcademicYear = AcademicYears.Format(FeeBillingPlanner.SessionStartYear(dueDate)),
        PeriodLabel = periodLabel,
        Description = description,
        DueDate = dueDate,
        TotalAmount = amount
    };

    public async Task<FeePaymentSummary> ApplyConcessionAsync(Guid feePaymentId, ConcessionRequest request, Guid? actorUserId, string? actorRole, CancellationToken ct = default)
    {
        var payment = await _payments.FindByIdAsync(feePaymentId, ct) ?? throw new KeyNotFoundException("Fee not found.");
        var outstanding = FeeMath.Pending(payment);
        if (request.Amount > outstanding)
            throw new InvalidOperationException($"That's more than the ₹{outstanding:0.##} still owed on this fee.");

        await using var tx = await _payments.BeginTransactionAsync(ct);
        if (await _payments.AddDiscountAsync(feePaymentId, request.Amount, ct) == 0)
            throw new InvalidOperationException("This fee changed meanwhile; reload and try again.");
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = payment.StudentId, FeePaymentId = payment.Id, Kind = FeeAdjustmentKinds.Concession,
            Amount = request.Amount, Reason = request.Reason.Trim(), ActorUserId = actorUserId, ActorRole = actorRole
        }, ct);
        await _accounts.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        payment.WaiverAmount += request.Amount;
        _logger.LogInformation("AUDIT actor={ActorUserId} action=FeePayment.Concession entity=FeePayment entityId={FeePaymentId} amount={Amount}",
            actorUserId, feePaymentId, request.Amount);
        return ToSummary(payment);
    }

    /// <summary>Appends the immutable ledger entry and bumps PaidAmount in one transaction, then
    /// publishes FeePaidEvent. Refuses an amount larger than what the fee still owes.</summary>
    private async Task<PaymentTransaction> AppendTransactionAsync(FeePayment payment, decimal amount, string paymentMethod, string? gatewayReference, CancellationToken ct)
    {
        if (amount <= 0)
            throw new InvalidOperationException("The amount must be more than zero.");

        var paidAtUtc = DateTime.UtcNow;
        var transaction = new PaymentTransaction
        {
            FeePaymentId = payment.Id,
            Amount = amount,
            ReceiptNumber = GenerateReceiptNumber(),
            PaymentMethod = paymentMethod,
            GatewayReference = gatewayReference,
            PaidAtUtc = paidAtUtc
        };

        await using (var tx = await _payments.BeginTransactionAsync(ct))
        {
            // Guarded, atomic increment -- never load PaidAmount, add in C#, and save the row back.
            if (await _payments.IncrementPaidAmountAsync(payment.Id, amount, paidAtUtc, ct) == 0)
            {
                var current = await _payments.FindByIdAsync(payment.Id, ct);
                throw new InvalidOperationException(
                    $"That's more than the ₹{(current is null ? 0 : FeeMath.Pending(current)):0.##} still owed on this fee.");
            }
            await _transactions.AddAsync(transaction, ct);
            await _transactions.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        payment.PaidAmount += amount;
        payment.LastPaidAtUtc = paidAtUtc;

        // Raised only after the payment is recorded; the notification handler confirms it.
        await _events.PublishAsync(new FeePaidEvent
        {
            PaymentId = transaction.Id,
            StudentId = payment.StudentId,
            AmountPaid = amount,
            Currency = "INR",
            ReceiptNumber = transaction.ReceiptNumber
        }, ct);

        return transaction;
    }

    /// <summary>Everything a student owes now (fees already due), never negative.</summary>
    private async Task<decimal> GetTotalPendingForStudentAsync(Guid studentId, CancellationToken ct)
    {
        var today = Today;
        var dues = await _payments.FindByStudentAsync(studentId, ct);
        return FeeMath.TotalPending(dues.Where(d => d.DueDate is null || d.DueDate <= today));
    }

    public async Task<string> GetReceiptDownloadUrlAsync(Guid paymentTransactionId, Guid? requiredStudentId = null, CancellationToken ct = default)
    {
        var transaction = await _transactions.FindByIdAsync(paymentTransactionId, ct)
            ?? throw new KeyNotFoundException("Payment transaction not found.");

        var payment = await _payments.FindByIdAsync(transaction.FeePaymentId, ct)
            ?? throw new KeyNotFoundException("Fee payment not found.");

        if (requiredStudentId.HasValue && payment.StudentId != requiredStudentId.Value)
            throw new UnauthorizedAccessException("You can only download your own receipts.");

        if (string.IsNullOrEmpty(transaction.ReceiptBlobPath)
            || !await _blobStorage.ExistsAsync(ContainerName, transaction.ReceiptBlobPath, ct))
        {
            // Everything this student still owes, not just the one fee this payment went
            // against -- otherwise clearing a single fee prints "Pending: 0" on the receipt
            // while other fees are still outstanding.
            var pendingAmount = await GetTotalPendingForStudentAsync(payment.StudentId, ct);
            var student = await _students.GetByIdAsync(payment.StudentId, ct);
            var details = new ReceiptDetails(student?.FullName, student?.AdmissionNumber,
                student is null ? null : SchoolClasses.NameOf(payment.ClassId ?? student.ClassId),
                FeeLedgerService.LabelOf(payment), payment.Category, payment.TotalAmount, FeeMath.Pending(payment));
            var document = new ReceiptDocument(transaction.ReceiptNumber, transaction.Amount, transaction.PaymentMethod, transaction.PaidAtUtc, payment.StudentId, _schoolName, pendingAmount, details);
            var pdfBytes = document.GeneratePdf();

            var blobPath = $"{payment.StudentId}/{transaction.Id}.pdf";
            using var stream = new MemoryStream(pdfBytes);
            await _blobStorage.UploadAsync(ContainerName, blobPath, stream, "application/pdf", ct);

            await _transactions.SetReceiptBlobPathAsync(transaction.Id, blobPath, ct);
            transaction.ReceiptBlobPath = blobPath;
        }

        return await _blobStorage.GetSasUrlAsync(ContainerName, transaction.ReceiptBlobPath!, TimeSpan.FromMinutes(15), ct);
    }

    public async Task RequestWaiverAsync(RequestWaiverRequest request, CancellationToken ct = default)
    {
        var payment = await _payments.FindByStudentAndStructureAsync(request.StudentId, request.FeeStructureId, ct)
            ?? throw new KeyNotFoundException("No fee payment record found for this student/fee structure.");

        // Step 1 of the two-step waiver workflow.
        await _payments.MarkWaiverRequestedAsync(payment.Id, ct);
    }

    public async Task ApproveWaiverAsync(Guid feePaymentId, ApproveWaiverRequest request, Guid approvedByUserId, CancellationToken ct = default)
    {
        var payment = await _payments.FindByIdAsync(feePaymentId, ct) ?? throw new KeyNotFoundException("Fee payment not found.");

        if (!payment.IsWaiverRequested)
            throw new InvalidOperationException("No waiver has been requested for this fee payment.");

        // Step 2 -- only Principal/Admin reaches this via [Authorize(Roles=...)].
        if (await _payments.ApproveWaiverAsync(feePaymentId, request.WaiverAmount, approvedByUserId, ct) == 0)
            throw new InvalidOperationException($"That's more than the ₹{FeeMath.Pending(payment):0.##} still owed on this fee.");
        await _accounts.AddAdjustmentAsync(new FeeAdjustment
        {
            StudentId = payment.StudentId, FeePaymentId = payment.Id, Kind = FeeAdjustmentKinds.WaiverApproved,
            Amount = request.WaiverAmount, Reason = "Waiver approved", ActorUserId = approvedByUserId
        }, ct);
        await _accounts.SaveChangesAsync(ct);

        _logger.LogInformation("AUDIT actor={ActorUserId} action=FeePayment.WaiverApproved entity=FeePayment entityId={FeePaymentId} waiverAmount={WaiverAmount}",
            approvedByUserId, feePaymentId, request.WaiverAmount);
    }

    public async Task<CollectionTotalsResponse> GetCollectionTotalsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var total = await _payments.GetTotalCollectedAsync(fromUtc, toUtc, ct);
        return new CollectionTotalsResponse(fromUtc, toUtc, total);
    }

    private static string GenerateReceiptNumber()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        return "RCPT-" + Convert.ToHexString(bytes);
    }

    public async Task<IReadOnlyList<FeePaymentSummary>> GetPaymentsForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var payments = await _payments.FindByStudentAsync(studentId, ct);
        return payments.Select(ToSummary).ToList();
    }

    public async Task<IReadOnlyList<FeePaymentSummary>> AssessDuesAsync(Guid studentId, AssessDuesRequest request, CancellationToken ct = default)
    {
        // The older free-form fee structures. Match the year by meaning ("2026-2027" is "2026-27").
        var structures = (await _structures.FindByClassAsync(request.ClassId, null, ct))
            .Where(s => AcademicYears.Same(s.AcademicYear, request.AcademicYear)).ToList();
        var existing = await _payments.FindByStudentAsync(studentId, ct);
        var existingStructureIds = existing.Where(p => p.FeeStructureId.HasValue).Select(p => p.FeeStructureId!.Value).ToHashSet();
        var hasOpeningBalance = existing.Any(p => p.FeeStructureId is null && p.Description != null && p.Description.StartsWith("Opening balance"));

        var toCreate = new List<FeePayment>();

        foreach (var structure in structures)
        {
            if (existingStructureIds.Contains(structure.Id)) continue; // already assessed
            toCreate.Add(new FeePayment
            {
                StudentId = studentId,
                FeeStructureId = structure.Id,
                ClassId = structure.ClassId,
                AcademicYear = AcademicYears.Normalize(structure.AcademicYear),
                PeriodLabel = structure.Name.Length <= 50 ? structure.Name : structure.Name[..50],
                DueDate = structure.DueDateUtc.Date,
                TotalAmount = structure.Amount
            });
        }

        if (request.OpeningBalance is > 0 && !hasOpeningBalance)
        {
            toCreate.Add(NewManualCharge(studentId, request.ClassId, request.OpeningBalance.Value, "Opening balance",
                string.IsNullOrWhiteSpace(request.OpeningBalanceDescription) ? "Opening balance (pending fee at admission)" : request.OpeningBalanceDescription,
                Today));
        }

        if (toCreate.Count > 0)
        {
            await _payments.AddRangeAsync(toCreate, ct);
            await _payments.SaveChangesAsync(ct);
            _logger.LogInformation("Assessed {Count} new due(s) for student={StudentId} class={ClassId}", toCreate.Count, studentId, request.ClassId);
        }

        var all = await _payments.FindByStudentAsync(studentId, ct);
        return all.Select(ToSummary).ToList();
    }

    public async Task<ClassPendingSummaryResponse> GetPendingSummaryByClassAsync(string classId, CancellationToken ct = default)
    {
        var dues = await _payments.FindByClassAsync(classId, ct);

        var perStudent = dues
            .GroupBy(p => p.StudentId)
            .Select(g =>
            {
                var totalDue = g.Sum(p => p.TotalAmount);
                var totalPaid = g.Sum(p => p.PaidAmount);
                var totalWaiver = g.Sum(p => p.WaiverAmount);
                var pending = FeeMath.Pending(totalDue, totalPaid, totalWaiver);
                return new StudentPendingSummary(g.Key, totalDue, totalPaid, totalWaiver, pending);
            })
            .OrderByDescending(s => s.Pending)
            .ToList();

        return new ClassPendingSummaryResponse(classId, perStudent.Sum(s => s.Pending), perStudent);
    }

    public async Task<IReadOnlyList<PaymentTransactionSummary>> GetTransactionsForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var dues = await _payments.FindByStudentAsync(studentId, ct);
        var duesById = dues.ToDictionary(p => p.Id);
        if (duesById.Count == 0) return Array.Empty<PaymentTransactionSummary>();

        var transactions = await _transactions.FindByFeePaymentIdsAsync(duesById.Keys, ct);
        return transactions
            .Select(t =>
            {
                var due = duesById[t.FeePaymentId];
                return new PaymentTransactionSummary(
                    t.Id, t.FeePaymentId, t.Amount, t.ReceiptNumber, t.PaymentMethod, t.PaidAtUtc,
                    due.PeriodLabel, due.Description ?? FeeLedgerService.LabelOf(due), due.FeeStructure?.Name);
            })
            .ToList();
    }

    private FeePaymentSummary ToSummary(FeePayment p)
    {
        var today = Today;
        return new(p.Id, p.StudentId, p.FeeStructureId, p.ClassId, p.Description, p.PeriodLabel,
            p.FeeStructure?.Name ?? FeeLedgerService.LabelOf(p), p.DueDate ?? p.FeeStructure?.DueDateUtc,
            p.TotalAmount, p.PaidAmount, p.WaiverAmount, p.Status,
            p.Category, p.AcademicYear, p.PeriodKey, FeeMath.Pending(p),
            p.DueDate is null || p.DueDate <= today, p.LastPaidAtUtc);
    }
}
