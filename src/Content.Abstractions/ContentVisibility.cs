namespace Content.Abstractions;

/// <summary>
/// Visibility level for content items.
/// </summary>
public enum ContentVisibility
{
    /// <summary>Anyone can see.</summary>
    Public = 0,

    /// <summary>Only friends of author can see.</summary>
    Friends = 1,

    /// <summary>Only the author can see.</summary>
    Private = 2
}
