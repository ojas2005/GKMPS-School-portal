using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Fee.DTOs;

public record CreateFeeStructureRequest([Required] string ClassId, [Required] string Name, [Required] decimal Amount, [Required] string AcademicYear, [Required] DateTime DueDateUtc);
public record FeeStructureSummary(Guid Id, string ClassId, string Name, decimal Amount, string AcademicYear, DateTime DueDateUtc);

// FeeStructureId (pay a class-structure due) and FeePaymentId (pay a specific due row,
// e.g. an ad-hoc opening balance that has no FeeStructure) are mutually exclusive --
// exactly one must be supplied. See PaymentsController.RecordPayment.
public record RecordPaymentRequest(
    [Required] Guid StudentId, Guid? FeeStructureId, Guid? FeePaymentId,
    [Required] decimal Amount, [Required] string PaymentMethod, string? GatewayReference);

public record FeePaymentSummary(
    Guid Id, Guid StudentId, Guid? FeeStructureId, string? ClassId, string? Description, string? PeriodLabel,
    string? FeeStructureName, DateTime? DueDateUtc,
    decimal TotalAmount, decimal PaidAmount, decimal WaiverAmount, string Status);

public record RequestWaiverRequest([Required] Guid StudentId, [Required] Guid FeeStructureId);
public record ApproveWaiverRequest([Required] decimal WaiverAmount);

public record CollectionTotalsResponse(DateTime? FromUtc, DateTime? ToUtc, decimal TotalCollected);

// Admitting/backfilling a student's dues: one row per unmet FeeStructure in their class
// for the given academic year, plus (at most once) an ad-hoc opening-balance row.
public record AssessDuesRequest(
    [Required] string ClassId, [Required] string AcademicYear,
    decimal? OpeningBalance, string? OpeningBalanceDescription);

public record StudentPendingSummary(Guid StudentId, decimal TotalDue, decimal TotalPaid, decimal TotalWaiver, decimal Pending);
public record ClassPendingSummaryResponse(string ClassId, decimal TotalPending, IReadOnlyList<StudentPendingSummary> Students);

// Owner-initiated fee submission: records a fully-paid, ad-hoc due in one step (a
// custom period + description instead of picking an existing FeeStructure) -- e.g.
// logging cash handed over for "May 2026 - Jun 2026" tuition. Immediately produces a
// receipt, same as any other payment.
public record SubmitFeePaymentRequest(
    [Required] string ClassId, [Required] decimal Amount, [Required] string PeriodLabel,
    string? Description, [Required] string PaymentMethod, string? GatewayReference);

public record FeeSubmissionResult(FeePaymentSummary Due, Guid TransactionId, string ReceiptNumber);

// Creates an unpaid ad-hoc due (no payment recorded, no receipt) -- e.g. a transport
// route's monthly fee added the moment a student is mapped to that route. Mirrors
// SubmitFeePaymentRequest minus the payment-method fields, since nothing was paid.
public record AddAdHocDueRequest(
    [Required] string ClassId, [Required] decimal Amount, [Required] string PeriodLabel, string? Description);

public record PaymentTransactionSummary(
    Guid Id, Guid FeePaymentId, decimal Amount, string ReceiptNumber, string PaymentMethod,
    DateTime PaidAtUtc, string? PeriodLabel, string? Description, string? FeeStructureName);
