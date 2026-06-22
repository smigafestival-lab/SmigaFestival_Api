namespace Smigafestival.Infrastructure.Services;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; init; } = string.Empty;

    public string ContainerName { get; init; } = string.Empty;
}
