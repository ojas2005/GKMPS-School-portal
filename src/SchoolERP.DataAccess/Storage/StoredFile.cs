namespace SchoolERP.DataAccess.Storage;

/// <summary>A generated PDF (receipt, report card, transfer certificate) kept in the database.</summary>
public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Container { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
