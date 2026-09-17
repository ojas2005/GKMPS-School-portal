using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Staff.Entities;

/// <summary>
/// Teacher/staff profile. LinkedUserId points back to the account in the Identity module.
/// EmployeeCode carries a unique index since it's the human-facing identifier used
/// on ID cards, payroll references, and subject-allocation records.
/// </summary>
public class StaffProfile : BaseEntity
{
    public Guid LinkedUserId { get; set; }

    public required string EmployeeCode { get; set; }
    public required string FullName { get; set; }
    public required string Designation { get; set; }   // Teacher | HeadOfDepartment | Principal | Accountant | Librarian | Support
    public string? SubjectsTaughtCsv { get; set; }      // simple CSV of subject codes; the Academic module owns the authoritative allocation
    public DateTime DateOfJoiningUtc { get; set; } = DateTime.UtcNow;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Active | OnLeave | Suspended | Resigned.</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Set when this teacher is the class teacher (head teacher) of a class/section.
    /// Only the class teacher of class X may upload class X's attendance; the Identity module embeds
    /// these into the teacher's JWT at login.</summary>
    public string? ClassTeacherOfClassId { get; set; }
    public string? ClassTeacherOfSectionId { get; set; }

    /// <summary>Set by the owner at onboarding (or later via PATCH .../salary). Pending
    /// salary for the current calendar month = MonthlySalary minus payouts recorded this
    /// month; null means no salary has been defined yet.</summary>
    public decimal? MonthlySalary { get; set; }
}
