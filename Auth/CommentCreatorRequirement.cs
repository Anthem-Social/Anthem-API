using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Requirements;

public class CommentCreatorRequirement : IAuthorizationRequirement { }

public class CommentCreatorHandler : AuthorizationHandler<CommentCreatorRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        CommentCreatorRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;
        
        if (!httpContext.Request.RouteValues.TryGetValue("commentId", out var c))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'commentId'.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        string userId = context.User.FindFirstValue("user_id")!;
        string commentId = c!.ToString()!;

        if (commentId.Split("#")[1] != userId)
        {
            var reason = new AuthorizationFailureReason(this, $"User {userId} is not the creator of Comment {commentId}.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
