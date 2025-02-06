using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Requirements;

public class SelfRequirement : IAuthorizationRequirement { }

public class SelfHandler : AuthorizationHandler<SelfRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context, 
        SelfRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;

        if (!httpContext.Request.RouteValues.TryGetValue("userId", out var userId))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'userId'.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        string claimsUserId = context.User.FindFirstValue("user_id")!;
        string routeUserId = userId!.ToString()!;

        if (claimsUserId != routeUserId)
        {
            var reason = new AuthorizationFailureReason(this, "Not self.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
