namespace SchoolERP.Examination.Storage;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default);
    Task<string> GetSasUrlAsync(string containerName, string blobPath, TimeSpan? validFor = null, CancellationToken ct = default);
}
