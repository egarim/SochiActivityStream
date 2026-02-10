namespace ActivityStream.Abstractions;

public enum ContentValidationError
{
    TenantIdRequired,
    AuthorRequired,
    AuthorIdRequired,
    PostBodyRequired,
    PostBodyTooLong,
    PostIdRequired,
    ActorRequired,
    ActorIdRequired,
    CommentBodyRequired,
    CommentBodyTooLong,
    CommentIdRequired,
    PostNotFound,
    PostUnauthorized,
    ParentCommentNotFound,
    ParentCommentWrongPost,
    CommentNotFound,
    CommentUnauthorized,
    TargetIdRequired,
    InvalidReactionType,
    TargetNotFound
}
