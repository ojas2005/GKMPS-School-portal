using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Fee.Handlers;

/// <summary>A student left (removed, transfer certificate) or came back: stop or resume billing.
/// The billing job checks the same thing on every run, so a missed event is corrected there.</summary>
public class StudentStatusChangedFeeHandler : IEventHandler<StudentStatusChangedEvent>
{
    private readonly IFeeAccountService _accounts;

    public StudentStatusChangedFeeHandler(IFeeAccountService accounts) => _accounts = accounts;

    public Task HandleAsync(StudentStatusChangedEvent e, CancellationToken ct) =>
        e.Status == "Active"
            ? _accounts.ReopenAsync(e.StudentId, ct)
            : _accounts.CloseAsync(e.StudentId, e.Status switch
            {
                "Removed" => "Student removed from the rolls",
                "TransferredOut" => "Student left (transfer certificate issued)",
                _ => $"Student is {e.Status}"
            }, ct);
}

/// <summary>A student moved class: fees not yet due follow the new class's rates; months from now
/// on are billed at the new class's tuition by the billing job. Past fees are never touched.</summary>
public class StudentClassChangedFeeHandler : IEventHandler<StudentClassChangedEvent>
{
    private readonly IFeeAccountService _accounts;

    public StudentClassChangedFeeHandler(IFeeAccountService accounts) => _accounts = accounts;

    public Task HandleAsync(StudentClassChangedEvent e, CancellationToken ct) =>
        _accounts.RepriceUpcomingAsync(e.StudentId, e.NewClassId, ct);
}

/// <summary>Route changes go into the student's fee history (the charges themselves follow on the next transport fee).</summary>
public class StudentTransportChangedFeeHandler : IEventHandler<StudentTransportChangedEvent>
{
    private readonly IFeeAccountService _accounts;

    public StudentTransportChangedFeeHandler(IFeeAccountService accounts) => _accounts = accounts;

    public Task HandleAsync(StudentTransportChangedEvent e, CancellationToken ct) =>
        _accounts.RecordTransportChangeAsync(e.StudentId, e.OldRouteName, e.NewRouteName, e.NewMonthlyFee, ct);
}
