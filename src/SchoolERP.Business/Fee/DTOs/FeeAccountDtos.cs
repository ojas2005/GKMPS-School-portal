using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Fee.DTOs;

/// <summary>A fee the admission will create (or has created).</summary>
public record FeeLine(string Category, string PeriodLabel, string? Description, string AcademicYear, string? PeriodKey,
    DateTime DueDate, decimal Amount, bool DueNow);

public record ExamFeeLine(string Period, string Label, decimal Amount, DateTime DueDate, bool Charged);

public record TransportChoice(Guid RouteId, string RouteName, decimal MonthlyFee);

/// <summary>
/// The fee summary for a class (and route) from a given date: what's charged once, every
/// month and per exam, which fees are created straight away, what is payable now and what
/// falls due later. The same shape is the admission form's live preview and the admission
/// result.
/// </summary>
public record FeeBreakdown(
    string AcademicYear, string ClassId, string ClassName, DateTime StartDate,
    decimal AdmissionFee, decimal MonthlyTuition,
    IReadOnlyList<ExamFeeLine> Exams, decimal ExamTotal,
    TransportChoice? Transport,
    decimal PreviousDues,
    IReadOnlyList<FeeLine> Lines,
    /// <summary>Due on the start date: admission fee, this month's tuition/transport, previous dues.</summary>
    decimal InitialPending,
    /// <summary>Created now but due later this session (exam fees).</summary>
    decimal UpcomingTotal,
    /// <summary>Tuition + transport, every month.</summary>
    decimal MonthlyTotal,
    string? Warning);

/// <summary>A fee entry as listed on the fee dashboard and statements.</summary>
public record FeeEntryDto(
    Guid Id, Guid StudentId, string? StudentName, string? AdmissionNumber,
    string? ClassId, string ClassName,
    string Category, string? AcademicYear, string? PeriodKey, string Label, string? Description,
    DateTime? DueDate,
    decimal TotalAmount, decimal PaidAmount, decimal DiscountAmount, decimal Pending,
    string Status, bool IsDue, bool IsOverdue, DateTime? LastPaidAtUtc,
    bool IsWaiverRequested, Guid? FeeStructureId);

public record StudentFeeBalanceDto(
    Guid StudentId, string? StudentName, string? AdmissionNumber, string? ClassId, string ClassName, string? StudentStatus,
    decimal Billed, decimal Paid, decimal Discount, decimal PendingDue, decimal Upcoming, DateTime? OldestUnpaidDueDate);

public record FeeCategoryTotalsDto(string Category, decimal Billed, decimal Paid, decimal Discount, decimal PendingDue, decimal Upcoming);

public record FeeOverviewDto(
    string AcademicYear,
    decimal CollectedThisSession, decimal CollectedThisMonth, decimal CollectedToday,
    decimal PendingDue, decimal Upcoming, decimal Discounts,
    IReadOnlyList<FeeCategoryTotalsDto> ByCategory,
    int OpenAccounts, int CurrentStudentsWithoutAccount);

public record MonthlyCollectionDto(int Year, int Month, string Label, decimal Billed, decimal Collected);

public record FeeAccountDto(bool IsOpen, DateTime BillingStartDate, DateTime? ClosedAtUtc, string? ClosedReason, DateTime? LastGeneratedAtUtc);

public record FeeStudentDto(Guid Id, string FullName, string? AdmissionNumber, string? ClassId, string ClassName, string? Status);

public record FeeTransportDto(Guid RouteId, string RouteName, decimal MonthlyFee, string? PickupPoint, bool RouteActive);

public record FeeTotalsDto(decimal Billed, decimal Paid, decimal Discount, decimal PendingDue, decimal Upcoming);

public record FeeHistoryDto(Guid Id, string Kind, decimal Amount, string? Reason, Guid? FeePaymentId, string? FeeLabel, string? ActorRole, DateTime CreatedAtUtc);

/// <summary>A student's complete fee statement.</summary>
public record FeeStatementDto(
    FeeStudentDto Student, FeeAccountDto? Account, FeeTransportDto? Transport, FeeTotalsDto Totals,
    IReadOnlyList<FeeEntryDto> Entries, IReadOnlyList<PaymentTransactionSummary> Payments, IReadOnlyList<FeeHistoryDto> History);

public record FeeEntryQuery(
    string? Keyword = null, string? ClassId = null, string? AcademicYear = null, string? Category = null,
    /// <summary>"yyyy-MM": fees falling due that month.</summary>
    string? Month = null,
    /// <summary>Pending, PartiallyPaid, Paid, Outstanding or Overdue.</summary>
    string? Status = null,
    bool IncludeUpcoming = true,
    int Page = 1, int PageSize = 50);

public record OpenFeeAccountRequest(
    /// <summary>First day to bill; today if omitted. Months before it are never charged.</summary>
    DateTime? BillingStartDate,
    bool ChargeAdmissionFee = false);

public record ConcessionRequest(
    [Range(0.01, 10_000_000), Display(Name = "Amount")] decimal Amount,
    [Required, StringLength(500, MinimumLength = 3), Display(Name = "Reason")] string Reason);

public record FeeGenerationResult(int AccountsChecked, int FeesCreated, int AccountsClosed, int AccountsReopened);

public record OpenAccountsResult(int Opened, int AlreadyOpen, int FeesCreated);

/// <summary>Everything admission needs to open a fee account (built by AdmissionService).</summary>
public record OpenFeeAccountCommand(
    Guid StudentId, string ClassId, DateTime BillingStartDate, Guid? RouteId,
    bool ChargeAdmissionFee, decimal? PreviousDues, Guid? ActorUserId, string? ActorRole);
