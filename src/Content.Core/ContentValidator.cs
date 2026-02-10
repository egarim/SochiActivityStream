using ActivityStream.Abstractions;

namespace Content.Core;

/// <summary>
/// Validation logic for content requests.
/// </summary>
public static class ContentValidator
{
    public static void ValidateCreatePost(CreatePostRequest request, ContentServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (request.Author == null)
            throw new ContentValidationException(ContentValidationError.AuthorRequired);

        if (string.IsNullOrWhiteSpace(request.Author.Id))
            throw new ContentValidationException(ContentValidationError.AuthorIdRequired);

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ContentValidationException(ContentValidationError.PostBodyRequired);

        if (request.Body.Length > options.MaxPostBodyLength)
            throw new ContentValidationException(ContentValidationError.PostBodyTooLong, nameof(request.Body));
    }

    public static void ValidateUpdatePost(UpdatePostRequest request, ContentServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (string.IsNullOrWhiteSpace(request.PostId))
            throw new ContentValidationException(ContentValidationError.PostIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);

        if (request.Body != null && request.Body.Length > options.MaxPostBodyLength)
            throw new ContentValidationException(ContentValidationError.PostBodyTooLong, nameof(request.Body));
    }

    public static void ValidateDeletePost(DeletePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (string.IsNullOrWhiteSpace(request.PostId))
            throw new ContentValidationException(ContentValidationError.PostIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);
    }

    public static void ValidateCreateComment(CreateCommentRequest request, ContentServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (request.Author == null)
            throw new ContentValidationException(ContentValidationError.AuthorRequired);

        if (string.IsNullOrWhiteSpace(request.Author.Id))
            throw new ContentValidationException(ContentValidationError.AuthorIdRequired);

        if (string.IsNullOrWhiteSpace(request.PostId))
            throw new ContentValidationException(ContentValidationError.PostIdRequired);

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ContentValidationException(ContentValidationError.CommentBodyRequired);

        if (request.Body.Length > options.MaxCommentBodyLength)
            throw new ContentValidationException(ContentValidationError.CommentBodyTooLong, nameof(request.Body));
    }

    public static void ValidateUpdateComment(UpdateCommentRequest request, ContentServiceOptions options)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (string.IsNullOrWhiteSpace(request.CommentId))
            throw new ContentValidationException(ContentValidationError.CommentIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ContentValidationException(ContentValidationError.CommentBodyRequired);

        if (request.Body.Length > options.MaxCommentBodyLength)
            throw new ContentValidationException(ContentValidationError.CommentBodyTooLong, nameof(request.Body));
    }

    public static void ValidateDeleteComment(DeleteCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (string.IsNullOrWhiteSpace(request.CommentId))
            throw new ContentValidationException(ContentValidationError.CommentIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);
    }

    public static void ValidateReact(ReactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);

        if (string.IsNullOrWhiteSpace(request.TargetId))
            throw new ContentValidationException(ContentValidationError.TargetIdRequired);

        if (!Enum.IsDefined(typeof(ReactionType), request.Type))
            throw new ContentValidationException(ContentValidationError.InvalidReactionType);
    }

    public static void ValidateRemoveReaction(RemoveReactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            throw new ContentValidationException(ContentValidationError.TenantIdRequired);

        if (request.Actor == null)
            throw new ContentValidationException(ContentValidationError.ActorRequired);

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
            throw new ContentValidationException(ContentValidationError.ActorIdRequired);

        if (string.IsNullOrWhiteSpace(request.TargetId))
            throw new ContentValidationException(ContentValidationError.TargetIdRequired);
    }
}
