using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Requirements;

public class LikeCreatorRequirement : IAuthorizationRequirement { }

public class LikeCreatorHandler : AuthorizationHandler<LikeCreatorRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        LikeCreatorRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;
        
        if (!httpContext.Request.RouteValues.TryGetValue("likeId", out var l))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'likeId'.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        string userId = context.User.FindFirstValue("id")!;
        string likeId = l!.ToString()!;

        if (likeId.Split("#")[1] != userId)
        {
            var reason = new AuthorizationFailureReason(this, $"User {userId} is not the creator of Like {likeId}.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
