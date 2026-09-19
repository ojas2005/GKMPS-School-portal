using SchoolERP.Business.Staff.DTOs;
using SchoolERP.DataAccess.Staff.Entities;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;
using SchoolERP.Business.Staff.Services.Interfaces;

namespace SchoolERP.Business.Staff.Services;

/// <summary>
/// Implements the two-step submit/approve leave workflow: Teacher/Staff submits,
/// Principal/Admin approves or rejects. Never both in one call.
/// </summary>
public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequests;
    private readonly IStaffRepository _staff;
    private readonly ILogger<LeaveRequestService> _logger;

    public LeaveRequestService(ILeaveRequestRepository leaveRequests, IStaffRepository staff, ILogger<LeaveRequestService> logger)
    {
        _leaveRequests = leaveRequests;
        _staff = staff;
        _logger = logger;
    }

    public async Task<LeaveRequestSummary> RequestAsync(Guid staffId, RequestLeaveRequest request, CancellationToken ct = default)
    {
        _ = await _staff.FindByIdAsync(staffId, ct) ?? throw new KeyNotFoundException("Staff member not found.");

        if (request.ToDateUtc < request.FromDateUtc)
            throw new InvalidOperationException("ToDate cannot be before FromDate.");

        // Duplicate-prevention guard: no overlapping leave request for the same staff member.
        if (await _leaveRequests.HasOverlappingRequestAsync(staffId, request.FromDateUtc, request.ToDateUtc, ct))
            throw new InvalidOperationException("An overlapping leave request already exists for this period.");

        var leaveRequest = new LeaveRequest
        {
            StaffId = staffId,
            LeaveType = request.LeaveType,
            FromDateUtc = request.FromDateUtc,
            ToDateUtc = request.ToDateUtc,
            Reason = request.Reason
        };

        await _leaveRequests.AddAsync(leaveRequest, ct);
        await _leaveRequests.SaveChangesAsync(ct);

        // Step 1 of the two-step workflow.
        await _leaveRequests.MarkSubmittedAsync(leaveRequest.Id, ct);
        leaveRequest.IsSubmitted = true;
        leaveRequest.SubmittedAtUtc = DateTime.UtcNow;

        _logger.LogInformation("Leave requested: {LeaveRequestId} for staff {StaffId}", leaveRequest.Id, staffId);

        return ToSummary(leaveRequest);
    }

    public async Task<LeaveRequestSummary> DecideAsync(Guid leaveRequestId, DecideLeaveRequest decision, Guid decidedByUserId, string decidedByRole, CancellationToken ct = default)
    {
        var leaveRequest = await _leaveRequests.FindByIdAsync(leaveRequestId, ct)
            ?? throw new KeyNotFoundException("Leave request not found.");

        if (!leaveRequest.IsSubmitted)
            throw new InvalidOperationException("This leave request has not been submitted yet.");

        if (leaveRequest.IsApproved || leaveRequest.IsRejected)
            throw new InvalidOperationException("This leave request has already been decided.");

        // Step 2 of the two-step workflow -- only Principal/Admin reaches this via [Authorize(Roles=...)].
        await _leaveRequests.DecideAsync(leaveRequestId, decision.Approve, decidedByUserId, decision.Note, ct);

        leaveRequest.IsApproved = decision.Approve;
        leaveRequest.IsRejected = !decision.Approve;
        leaveRequest.DecidedAtUtc = DateTime.UtcNow;
        leaveRequest.DecidedByUserId = decidedByUserId;
        leaveRequest.DecisionNote = decision.Note;

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=LeaveRequest.Decide entity=LeaveRequest entityId={LeaveRequestId} decision={Approved}",
            decidedByUserId, decidedByRole, leaveRequestId, decision.Approve);

        return ToSummary(leaveRequest);
    }

    private static LeaveRequestSummary ToSummary(LeaveRequest l) =>
        new(l.Id, l.StaffId, l.LeaveType, l.FromDateUtc, l.ToDateUtc, l.IsSubmitted, l.IsApproved, l.IsRejected);
}
