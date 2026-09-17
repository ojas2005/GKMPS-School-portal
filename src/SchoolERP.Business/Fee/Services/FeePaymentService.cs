using System.Security.Cryptography;
using QuestPDF.Fluent;
using SchoolERP.Business.Fee.Documents;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.DataAccess.Storage;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Fee.Services;

/// <summary>
/// Every payment: creates (or reuses) the FeePayment tracker row, appends an immutable
/// PaymentTransaction ledger entry, atomically bumps PaidAmount via ExecuteUpdateAsync,
/// generates a QuestPDF receipt uploaded to Blob (SAS-delivered), and publishes
/// FeePaidEvent for the Notification module to react to.
/// </summary>
public class FeePaymentService : IFeePaymentService
{
    private readonly IFeePaymentRepository _payments;
    private readonly IFeeStructureRepository _structures;
    private readonly IPaymentTransactionRepository _transactions;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventPublisher _events;
    private readonly ILogger<FeePaymentService> _logger;
    private readonly string _schoolName;

    private const string ContainerName = "fee-receipts";

    public FeePaymentService(
        IFeePaymentRepository payments,
        IFeeStructureRepository structures,
        IPaymentTransactionRepository transactions,
        IBlobStorageService blobStorage,
        IEventPublisher events,
        IConfiguration configuration,
        ILogger<FeePaymentService> logger)
    {
        _payments = payments;
        _structures = structures;
        _transactions = transactions;
        _blobStorage = blobStorage;
        _events = events;
        _logger = logger;
        _schoolName = configuration["School:Name"] ?? "School";
    }

    public async Task<FeePaymentSummary> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        // Two ways to identify what's being paid: a specific due row directly (needed for
        // ad-hoc dues like an opening balance, which have no FeeStructure), or the
        // class-structure it's linked to (existing behavior -- creates the row on first payment).
        FeePayment? payment;
        if (request.FeePaymentId.HasValue)
        {
            payment = await _payments.FindByIdAsync(request.FeePaymentId.Value, ct)
                ?? throw new KeyNotFoundException("Fee due not found.");
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
        // Always a brand-new ad-hoc due -- the owner is logging money received for a
        // custom period, not paying down an existing FeeStructure-linked due.
        var payment = new FeePayment
        {
            StudentId = studentId,
            FeeStructureId = null,
            ClassId = request.ClassId,
            PeriodLabel = request.PeriodLabel,
            Description = request.Description,
            TotalAmount = request.Amount
        };
        await _payments.AddAsync(payment, ct);
        await _payments.SaveChangesAsync(ct);

        var transaction = await AppendTransactionAsync(payment, request.Amount, request.PaymentMethod, request.GatewayReference, ct);

        _logger.LogInformation("Fee submitted: {ReceiptNumber} amount={Amount} period={Period} student={StudentId}",
            transaction.ReceiptNumber, request.Amount, request.PeriodLabel, studentId);

        return new FeeSubmissionResult(ToSummary(payment), transaction.Id, transaction.ReceiptNumber);
    }

    public async Task<FeePaymentSummary> AddAdHocDueAsync(Guid studentId, AddAdHocDueRequest request, CancellationToken ct = default)
    {
        // Unlike SubmitPaymentAsync, no AppendTransactionAsync call -- nothing was paid,
        // so PaidAmount stays at its default 0 and no transaction/receipt/event is produced.
        var payment = new FeePayment
        {
            StudentId = studentId,
            FeeStructureId = null,
            ClassId = request.ClassId,
            PeriodLabel = request.PeriodLabel,
            Description = request.Description,
            TotalAmount = request.Amount
        };
        await _payments.AddAsync(payment, ct);
        await _payments.SaveChangesAsync(ct);

        _logger.LogInformation("Ad-hoc due added: amount={Amount} period={Period} student={StudentId}",
            request.Amount, request.PeriodLabel, studentId);

        return ToSummary(payment);
    }

    /// <summary>Shared by RecordPaymentAsync/SubmitPaymentAsync: appends the immutable ledger
    /// entry, atomically bumps PaidAmount, and publishes FeePaidEvent for Reporting/Notification.</summary>
    private async Task<PaymentTransaction> AppendTransactionAsync(FeePayment payment, decimal amount, string paymentMethod, string? gatewayReference, CancellationToken ct)
    {
        var receiptNumber = GenerateReceiptNumber();
        var transaction = new PaymentTransaction
        {
            FeePaymentId = payment.Id,
            Amount = amount,
            ReceiptNumber = receiptNumber,
            PaymentMethod = paymentMethod,
            GatewayReference = gatewayReference
        };
        await _transactions.AddAsync(transaction, ct);
        await _transactions.SaveChangesAsync(ct);

        // Atomic increment -- never load PaidAmount, add in C#, and save the whole row back.
        await _payments.IncrementPaidAmountAsync(payment.Id, amount, ct);
        payment.PaidAmount += amount;

        // Raised only after the payment is recorded; the notification handler confirms it.
        await _events.PublishAsync(new FeePaidEvent
        {
            PaymentId = transaction.Id,
            StudentId = payment.StudentId,
            AmountPaid = amount,
            Currency = "INR",
            ReceiptNumber = receiptNumber
        }, ct);

        return transaction;
    }

    /// <summary>Everything a student still owes across all their dues (never negative).</summary>
    private async Task<decimal> GetTotalPendingForStudentAsync(Guid studentId, CancellationToken ct)
    {
        var dues = await _payments.FindByStudentAsync(studentId, ct);
        return FeeMath.TotalPending(dues);
    }

    public async Task<string> GetReceiptDownloadUrlAsync(Guid paymentTransactionId, Guid? requiredStudentId = null, CancellationToken ct = default)
    {
        var transaction = await _transactions.FindByIdAsync(paymentTransactionId, ct)
            ?? throw new KeyNotFoundException("Payment transaction not found.");

        var payment = await _payments.FindByIdAsync(transaction.FeePaymentId, ct)
            ?? throw new KeyNotFoundException("Fee payment not found.");

        if (requiredStudentId.HasValue && payment.StudentId != requiredStudentId.Value)
            throw new UnauthorizedAccessException("You can only download your own receipts.");

        if (string.IsNullOrEmpty(transaction.ReceiptBlobPath))
        {

            // Everything this student still owes, not just the one due this payment went
            // against -- otherwise clearing a single fee prints "Pending Fee: 0" on the
            // receipt while other dues are still outstanding.
            var pendingAmount = await GetTotalPendingForStudentAsync(payment.StudentId, ct);
            var document = new ReceiptDocument(transaction.ReceiptNumber, transaction.Amount, transaction.PaymentMethod, transaction.PaidAtUtc, payment.StudentId, _schoolName, pendingAmount);
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
        await _payments.ApproveWaiverAsync(feePaymentId, request.WaiverAmount, approvedByUserId, ct);

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
        var structures = await _structures.FindByClassAsync(request.ClassId, request.AcademicYear, ct);
        var existing = await _payments.FindByStudentAsync(studentId, ct);
        var existingStructureIds = existing.Where(p => p.FeeStructureId.HasValue).Select(p => p.FeeStructureId!.Value).ToHashSet();
        var hasOpeningBalance = existing.Any(p => p.FeeStructureId is null);

        var toCreate = new List<FeePayment>();

        foreach (var structure in structures)
        {
            if (existingStructureIds.Contains(structure.Id)) continue; // already assessed
            toCreate.Add(new FeePayment
            {
                StudentId = studentId,
                FeeStructureId = structure.Id,
                ClassId = structure.ClassId,
                TotalAmount = structure.Amount
            });
        }

        if (request.OpeningBalance is > 0 && !hasOpeningBalance)
        {
            toCreate.Add(new FeePayment
            {
                StudentId = studentId,
                FeeStructureId = null,
                ClassId = request.ClassId,
                Description = string.IsNullOrWhiteSpace(request.OpeningBalanceDescription)
                    ? "Opening balance (pending fee at admission)"
                    : request.OpeningBalanceDescription,
                TotalAmount = request.OpeningBalance.Value
            });
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
                    due.PeriodLabel, due.Description, due.FeeStructure?.Name);
            })
            .ToList();
    }

    private static FeePaymentSummary ToSummary(FeePayment p) =>
        new(p.Id, p.StudentId, p.FeeStructureId, p.ClassId, p.Description, p.PeriodLabel,
            p.FeeStructure?.Name, p.FeeStructure?.DueDateUtc,
            p.TotalAmount, p.PaidAmount, p.WaiverAmount, p.Status);
}
