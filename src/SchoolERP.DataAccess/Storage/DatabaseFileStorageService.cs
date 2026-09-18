using Microsoft.EntityFrameworkCore;

namespace SchoolERP.DataAccess.Storage;

public record StoredFileContent(byte[] Content, string ContentType);

/// <summary>Reads a stored file back for the download endpoint.</summary>
public interface IFileStoreReader
{
    Task<StoredFileContent?> ReadAsync(string containerName, string path, CancellationToken ct = default);
}

/// <summary>
/// Keeps generated PDFs in the database instead of Azure Blob Storage, so file storage costs
/// nothing. Downloads go through signed, 15-minute links served by the API itself.
/// </summary>
public class DatabaseFileStorageService : IBlobStorageService, IFileStoreReader
{
    private readonly FileStoreDbContext _db;
    private readonly FileLinkSigner _signer;

    public DatabaseFileStorageService(FileStoreDbContext db, FileLinkSigner signer)
    {
        _db = db;
        _signer = signer;
    }

    public async Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);

        // Re-generating a document (e.g. a report card after marks change) replaces it.
        var existing = await _db.Files.FirstOrDefaultAsync(f => f.Container == containerName && f.Path == blobPath, ct);
        if (existing is null)
        {
            _db.Files.Add(new StoredFile { Container = containerName, Path = blobPath, ContentType = contentType, Content = buffer.ToArray() });
        }
        else
        {
            existing.Content = buffer.ToArray();
            existing.ContentType = contentType;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return blobPath;
    }

    public Task<string> GetSasUrlAsync(string containerName, string blobPath, TimeSpan? validFor = null, CancellationToken ct = default) =>
        Task.FromResult(_signer.CreateLink(containerName, blobPath, validFor ?? TimeSpan.FromMinutes(15), DateTimeOffset.UtcNow));

    public Task<bool> ExistsAsync(string containerName, string blobPath, CancellationToken ct = default) =>
        _db.Files.AnyAsync(f => f.Container == containerName && f.Path == blobPath, ct);

    public async Task<StoredFileContent?> ReadAsync(string containerName, string path, CancellationToken ct = default)
    {
        var file = await _db.Files.AsNoTracking()
            .Where(f => f.Container == containerName && f.Path == path)
            .Select(f => new StoredFileContent(f.Content, f.ContentType))
            .FirstOrDefaultAsync(ct);
        return file;
    }
}
