namespace Search.Abstractions;

/// <summary>
/// A filter condition for search queries.
/// </summary>
public sealed class SearchFilter
{
    /// <summary>
    /// Field name to filter on.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Filter operator.
    /// </summary>
    public SearchFilterOperator Operator { get; set; } = SearchFilterOperator.Equals;

    /// <summary>
    /// Value(s) to filter by.
    /// </summary>
    public required object Value { get; set; }
}

/// <summary>
/// Filter operators for search queries.
/// </summary>
public enum SearchFilterOperator
{
    /// <summary>
    /// Exact match (keyword fields).
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Not equal.
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// Contains any of the values (keyword arrays).
    /// </summary>
    In = 2,

    /// <summary>
    /// Does not contain any of the values.
    /// </summary>
    NotIn = 3,

    /// <summary>
    /// Greater than (numeric/date).
    /// </summary>
    GreaterThan = 4,

    /// <summary>
    /// Greater than or equal (numeric/date).
    /// </summary>
    GreaterThanOrEqual = 5,

    /// <summary>
    /// Less than (numeric/date).
    /// </summary>
    LessThan = 6,

    /// <summary>
    /// Less than or equal (numeric/date).
    /// </summary>
    LessThanOrEqual = 7,

    /// <summary>
    /// Range (between two values, inclusive).
    /// </summary>
    Between = 8,

    /// <summary>
    /// Prefix match (starts with).
    /// </summary>
    StartsWith = 9,

    /// <summary>
    /// Field exists (not null).
    /// </summary>
    Exists = 10
}
