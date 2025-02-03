using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Authorization;

public class PostCreatorRequirement : IAuthorizationRequirement { }

public class PostCreatorHandler : AuthorizationHandler<PostCreatorRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        PostCreatorRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;
        
        if (!httpContext.Request.RouteValues.TryGetValue("postId", out var p))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'postId'.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        string userId = context.User.FindFirstValue("id")!;
        string postId = p!.ToString()!;

        if (postId.Split("#")[1] != userId)
        {
            var reason = new AuthorizationFailureReason(this, $"User {userId} is not the creator of Post {postId}.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
