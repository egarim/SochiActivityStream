namespace Content.Abstractions;

/// <summary>
/// Validation error codes for content operations.
/// </summary>
public enum ContentValidationError
{
    // General
    TenantIdRequired,

    // Post
    PostIdRequired,
    PostNotFound,
    PostBodyRequired,
    PostBodyTooLong,
    PostUnauthorized,

    // Comment
    CommentIdRequired,
    CommentNotFound,
    CommentBodyRequired,
    CommentBodyTooLong,
    CommentUnauthorized,
    ParentCommentNotFound,
    ParentCommentWrongPost,

    // Reaction
    TargetIdRequired,
    TargetNotFound,
    InvalidReactionType,

    // Actor/Author
    AuthorRequired,
    ActorRequired,
    AuthorIdRequired,
    ActorIdRequired
}
