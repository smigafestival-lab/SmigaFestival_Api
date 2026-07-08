using Smigafestival.Application.Common.Models;

namespace Smigafestival.Application.Abstractions;

public interface IBlobStorageService
{
    Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);


    Uri GetBlobSasUri(string blobName, TimeSpan expiry);
}
