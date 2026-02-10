using System;

namespace ActivityStream.Abstractions;

public sealed class ContentValidationException : Exception
{
    public ContentValidationError Error { get; }
    public string? Field { get; }

    public ContentValidationException(ContentValidationError error, string? field = null)
        : base(error.ToString())
    {
        Error = error;
        Field = field;
    }
}
