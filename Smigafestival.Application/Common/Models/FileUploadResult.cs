namespace Smigafestival.Application.Common.Models;

public sealed record FileUploadResult(
    string BlobName,
    string FileName,
    string ContentType,
    long Size,
    Uri sasUri);
