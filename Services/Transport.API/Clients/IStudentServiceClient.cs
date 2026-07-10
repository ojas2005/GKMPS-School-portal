namespace SchoolERP.Transport.Clients;

public interface IStudentServiceClient
{
    Task<StudentLookupResult?> GetByIdAsync(Guid studentId, CancellationToken ct = default);
}

public record StudentLookupResult(Guid Id, string FullName, string AdmissionNumber);
