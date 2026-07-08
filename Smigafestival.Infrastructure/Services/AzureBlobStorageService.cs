using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Storage;
using Smigafestival.Application.Abstractions;
using Smigafestival.Application.Common.Models;

namespace Smigafestival.Infrastructure.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(BlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Blob storage connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ContainerName))
        {
            throw new InvalidOperationException("Blob storage container name is not configured.");
        }

        var serviceClient = new BlobServiceClient(options.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(options.ContainerName);
    }

    public async Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var safeFileName = Path.GetFileName(fileName);
        var blobName = $"{Guid.NewGuid():N}-{safeFileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            }
        };

        

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        var size = content.CanSeek ? content.Length : 0;
        var sasUri = GetBlobSasUri(blobName, TimeSpan.FromHours(1));
        return new FileUploadResult(
            blobName,
            safeFileName,
            uploadOptions.HttpHeaders.ContentType,
            size,
            sasUri
            );
    }


public Uri GetBlobSasUri(string blobName, TimeSpan expiry)
{
    BlobClient blobClient = _containerClient.GetBlobClient(blobName);

    BlobSasBuilder sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = _containerClient.Name,
        BlobName = blobName,
        Resource = "b",
        ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
    };

    sasBuilder.SetPermissions(BlobSasPermissions.Read);

    Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

    return sasUri;
}

}
