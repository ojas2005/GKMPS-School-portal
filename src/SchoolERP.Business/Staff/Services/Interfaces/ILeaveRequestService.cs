using SchoolERP.Business.Staff.DTOs;

namespace SchoolERP.Business.Staff.Services.Interfaces;

public interface ILeaveRequestService
{
    Task<LeaveRequestSummary> RequestAsync(Guid staffId, RequestLeaveRequest request, CancellationToken ct = default);
    Task<LeaveRequestSummary> DecideAsync(Guid leaveRequestId, DecideLeaveRequest decision, Guid decidedByUserId, string decidedByRole, CancellationToken ct = default);
}
