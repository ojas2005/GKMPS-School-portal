using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace SchoolERP.DataAccess.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string? _publicBaseUrl;

    /// <summary>
    /// BlobStorage:PublicBaseUrl rewrites the host/port of generated SAS URIs -- needed
    /// in local Docker dev, where the connection string's BlobEndpoint points at the
    /// "azurite" container hostname (reachable from other containers, not from the
    /// developer's browser), while the same Azurite port is also published to the host
    /// as e.g. http://localhost:10000/devstoreaccount1. Only the scheme/host/port are
    /// swapped; the signed path and SAS query string are left untouched, so the token
    /// stays valid. Unset in production, where the real Storage account's public
    /// endpoint is already browser-reachable.
    /// </summary>
    public AzureBlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _publicBaseUrl = configuration["BlobStorage:PublicBaseUrl"];
    }

    public async Task<string> UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(content, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobPath;
    }

    public Task<string> GetSasUrlAsync(string containerName, string blobPath, TimeSpan? validFor = null, CancellationToken ct = default)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobPath);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException("Blob client is not authorized to generate SAS URIs (requires account key or user delegation key).");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(validFor ?? TimeSpan.FromMinutes(15))
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(RewriteForPublicAccess(sasUri));
    }

    public async Task<bool> ExistsAsync(string containerName, string blobPath, CancellationToken ct = default)
    {
        var blob = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobPath);
        return (await blob.ExistsAsync(ct)).Value;
    }

    public async Task<int> DeleteByPrefixAsync(string containerName, string prefix, CancellationToken ct = default)
    {
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        if (!(await container.ExistsAsync(ct)).Value) return 0;
        var deleted = 0;
        await foreach (var blob in container.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
        {
            await container.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: ct);
            deleted++;
        }
        return deleted;
    }

    private string RewriteForPublicAccess(Uri sasUri)
    {
        if (string.IsNullOrEmpty(_publicBaseUrl)) return sasUri.ToString();

        var publicBase = new Uri(_publicBaseUrl);
        var builder = new UriBuilder(sasUri) { Scheme = publicBase.Scheme, Host = publicBase.Host, Port = publicBase.Port };
        return builder.Uri.ToString();
    }
}
