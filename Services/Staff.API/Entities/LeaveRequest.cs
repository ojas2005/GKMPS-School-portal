using SchoolERP.Shared.Entities;

namespace SchoolERP.Staff.Entities;

/// <summary>
/// Two-step submit/approve workflow: the staff member (or an Admin on their behalf)
/// submits the request (IsSubmitted), then the Principal/Admin approves or rejects it.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public Guid StaffId { get; set; }
    public StaffProfile? Staff { get; set; }

    public required string LeaveType { get; set; } // Sick | Casual | Earned | Maternity | Paternity | Unpaid
    public DateTime FromDateUtc { get; set; }
    public DateTime ToDateUtc { get; set; }
    public required string Reason { get; set; }

    public bool IsSubmitted { get; set; } = false;
    public DateTime? SubmittedAtUtc { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsRejected { get; set; } = false;
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionNote { get; set; }
}
