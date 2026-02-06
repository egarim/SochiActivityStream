namespace Search.Abstractions;

/// <summary>
/// Validation errors for search operations.
/// </summary>
public enum SearchValidationError
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
    /// Document type is required.
    /// </summary>
    DocumentTypeRequired,

    /// <summary>
    /// Document type exceeds maximum length.
    /// </summary>
    DocumentTypeTooLong,

    /// <summary>
    /// Document ID is required.
    /// </summary>
    IdRequired,

    /// <summary>
    /// Document ID exceeds maximum length.
    /// </summary>
    IdTooLong,

    /// <summary>
    /// Query exceeds maximum length.
    /// </summary>
    QueryTooLong,

    /// <summary>
    /// Autocomplete prefix is required.
    /// </summary>
    PrefixRequired,

    /// <summary>
    /// Autocomplete prefix exceeds maximum length.
    /// </summary>
    PrefixTooLong,

    /// <summary>
    /// Limit is out of valid range.
    /// </summary>
    LimitOutOfRange,

    /// <summary>
    /// Filter value is invalid.
    /// </summary>
    InvalidFilterValue,

    /// <summary>
    /// Unknown field referenced.
    /// </summary>
    UnknownField
}
