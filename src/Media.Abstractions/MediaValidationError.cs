namespace Media.Abstractions;

/// <summary>
/// Validation errors for media operations.
/// </summary>
public enum MediaValidationError
{
    /// <summary>
    /// Tenant ID is required.
    /// </summary>
    TenantIdRequired,

    /// <summary>
    /// Tenant ID exceeds maximum length.
    /// </summary>
    TenantIdTooLong,

    /// <summary>
    /// Owner entity is required.
    /// </summary>
    OwnerRequired,

    /// <summary>
    /// File name is required.
    /// </summary>
    FileNameRequired,

    /// <summary>
    /// File name exceeds maximum length.
    /// </summary>
    FileNameTooLong,

    /// <summary>
    /// File name contains invalid characters.
    /// </summary>
    FileNameInvalid,

    /// <summary>
    /// Content type is required.
    /// </summary>
    ContentTypeRequired,

    /// <summary>
    /// Content type is not in the allowed list.
    /// </summary>
    ContentTypeNotAllowed,

    /// <summary>
    /// File exceeds maximum size limit.
    /// </summary>
    FileTooLarge,

    /// <summary>
    /// Alt text exceeds maximum length.
    /// </summary>
    AltTextTooLong,

    /// <summary>
    /// Media item was not found.
    /// </summary>
    MediaNotFound,

    /// <summary>
    /// Media item is not in Pending status.
    /// </summary>
    MediaNotPending,

    /// <summary>
    /// Media item is already deleted.
    /// </summary>
    MediaAlreadyDeleted,

    /// <summary>
    /// Upload has not been confirmed (blob does not exist).
    /// </summary>
    UploadNotConfirmed,

    /// <summary>
    /// Actor is not authorized to perform this action.
    /// </summary>
    NotAuthorized
}
