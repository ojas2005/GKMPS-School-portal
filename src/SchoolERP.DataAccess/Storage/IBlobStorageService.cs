namespace SchoolERP.DataAccess.Storage;

/// <summary>
/// Where generated PDFs live: the database by default (free), or Azure Blob Storage when
/// FileStorage:Provider is "AzureBlob". Every read a client performs goes through a
/// time-limited signed link -- files are never publicly accessible directly.
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Generates a SAS URL valid for the given lifetime (default short-lived, e.g. 15 minutes).</summary>
    Task<string> GetSasUrlAsync(string containerName, string blobPath, TimeSpan? validFor = null, CancellationToken ct = default);

    /// <summary>Whether the file is still there -- a cached path can outlive its file after a storage switch.</summary>
    Task<bool> ExistsAsync(string containerName, string blobPath, CancellationToken ct = default);

    /// <summary>Deletes every file whose path starts with <paramref name="prefix"/> (e.g. one student's folder).</summary>
    Task<int> DeleteByPrefixAsync(string containerName, string prefix, CancellationToken ct = default);
}
