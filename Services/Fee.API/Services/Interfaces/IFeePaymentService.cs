using SchoolERP.Fee.DTOs;

namespace SchoolERP.Fee.Services.Interfaces;

public interface IFeePaymentService
{
    Task<FeePaymentSummary> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FeePaymentSummary>> GetPaymentsForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<string> GetReceiptDownloadUrlAsync(Guid paymentTransactionId, CancellationToken ct = default);

    Task RequestWaiverAsync(RequestWaiverRequest request, CancellationToken ct = default);
    Task ApproveWaiverAsync(Guid feePaymentId, ApproveWaiverRequest request, Guid approvedByUserId, CancellationToken ct = default);

    Task<CollectionTotalsResponse> GetCollectionTotalsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    /// <summary>Creates any missing dues for a student: one row per FeeStructure in their
    /// class/year not yet assessed, plus (at most once) an ad-hoc opening-balance row.
    /// Safe to call repeatedly -- already-assessed dues are left untouched.</summary>
    Task<IReadOnlyList<FeePaymentSummary>> AssessDuesAsync(Guid studentId, AssessDuesRequest request, CancellationToken ct = default);

    /// <summary>Per-student pending totals for every student with a due tagged to this class.</summary>
    Task<ClassPendingSummaryResponse> GetPendingSummaryByClassAsync(string classId, CancellationToken ct = default);

    /// <summary>Owner-initiated: creates a fully-paid ad-hoc due for a custom period +
    /// description in one step and immediately produces its receipt.</summary>
    Task<FeeSubmissionResult> SubmitPaymentAsync(Guid studentId, SubmitFeePaymentRequest request, CancellationToken ct = default);

    /// <summary>Every receipt (payment transaction) across all of a student's dues, newest first.</summary>
    Task<IReadOnlyList<PaymentTransactionSummary>> GetTransactionsForStudentAsync(Guid studentId, CancellationToken ct = default);
}
