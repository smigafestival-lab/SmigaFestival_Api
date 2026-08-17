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
    public TimeSpan DefaultSasExpiry => TimeSpan.FromHours(1);

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
        var sasUri = GetBlobSasUri(blobName, DefaultSasExpiry);
        return new FileUploadResult(
            blobName,
            safeFileName,
            uploadOptions.HttpHeaders.ContentType,
            size,
            blobClient.Uri,
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

    public Uri GetBlobSasUriForUrl(string blobUrl, TimeSpan expiry)
    {
        var blobName = ExtractBlobName(blobUrl);
        return GetBlobSasUri(blobName, expiry);
    }

    private static string ExtractBlobName(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            throw new ArgumentException("Blob URL is required.", nameof(blobUrl));
        }

        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            return blobUrl.Trim();
        }

        var path = uri.AbsolutePath.Trim('/');
        var slashIndex = path.IndexOf('/');
        if (slashIndex < 0 || slashIndex == path.Length - 1)
        {
            throw new ArgumentException("Blob URL does not contain a valid blob name.", nameof(blobUrl));
        }

        return Uri.UnescapeDataString(path[(slashIndex + 1)..]);
    }
}
