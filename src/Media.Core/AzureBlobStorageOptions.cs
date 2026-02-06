namespace Media.Core;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public sealed class AzureBlobStorageOptions
{
    /// <summary>
    /// Azure Storage connection string.
    /// For Azurite: "UseDevelopmentStorage=true"
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Container name. Default: "media".
    /// </summary>
    public string ContainerName { get; set; } = "media";

    /// <summary>
    /// Whether to create container if it doesn't exist.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;
}
