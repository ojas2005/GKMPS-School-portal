namespace SchoolERP.Student.Storage;

/// <summary>
/// Wraps Azure Blob Storage. Every read a client performs goes through a time-limited
/// SAS URL -- blobs are never made publicly accessible directly.
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Generates a SAS URL valid for the given lifetime (default short-lived, e.g. 15 minutes).</summary>
    Task<string> GetSasUrlAsync(string containerName, string blobPath, TimeSpan? validFor = null, CancellationToken ct = default);
}
