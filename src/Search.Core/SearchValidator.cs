using Search.Abstractions;

namespace Search.Core;

/// <summary>
/// Validates search requests and documents.
/// </summary>
public static class SearchValidator
{
    private const int MaxTenantIdLength = 100;
    private const int MaxDocumentTypeLength = 50;
    private const int MaxIdLength = 100;
    private const int MaxQueryLength = 500;
    private const int MaxPrefixLength = 100;
    private const int MaxLimit = 100;

    /// <summary>
    /// Validates a search request.
    /// </summary>
    public static void ValidateSearchRequest(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new SearchValidationException(SearchValidationError.TenantIdRequired);

        if (request.TenantId.Length > MaxTenantIdLength)
            throw new SearchValidationException(SearchValidationError.TenantIdTooLong);

        if (request.Query != null && request.Query.Length > MaxQueryLength)
            throw new SearchValidationException(SearchValidationError.QueryTooLong);

        if (request.Limit < 1 || request.Limit > MaxLimit)
            throw new SearchValidationException(SearchValidationError.LimitOutOfRange);
    }

    /// <summary>
    /// Validates an autocomplete request.
    /// </summary>
    public static void ValidateAutocompleteRequest(AutocompleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new SearchValidationException(SearchValidationError.TenantIdRequired);

        if (request.TenantId.Length > MaxTenantIdLength)
            throw new SearchValidationException(SearchValidationError.TenantIdTooLong);

        if (string.IsNullOrWhiteSpace(request.Prefix))
            throw new SearchValidationException(SearchValidationError.PrefixRequired);

        if (request.Prefix.Length > MaxPrefixLength)
            throw new SearchValidationException(SearchValidationError.PrefixTooLong);

        if (request.Limit < 1 || request.Limit > MaxLimit)
            throw new SearchValidationException(SearchValidationError.LimitOutOfRange);
    }

    /// <summary>
    /// Validates a search document for indexing.
    /// </summary>
    public static void ValidateDocument(SearchDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.TenantId))
            throw new SearchValidationException(SearchValidationError.TenantIdRequired);

        if (document.TenantId.Length > MaxTenantIdLength)
            throw new SearchValidationException(SearchValidationError.TenantIdTooLong);

        if (string.IsNullOrWhiteSpace(document.DocumentType))
            throw new SearchValidationException(SearchValidationError.DocumentTypeRequired);

        if (document.DocumentType.Length > MaxDocumentTypeLength)
            throw new SearchValidationException(SearchValidationError.DocumentTypeTooLong);

        if (string.IsNullOrWhiteSpace(document.Id))
            throw new SearchValidationException(SearchValidationError.IdRequired);

        if (document.Id.Length > MaxIdLength)
            throw new SearchValidationException(SearchValidationError.IdTooLong);
    }
}
