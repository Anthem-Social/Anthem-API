using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Authorization;

public class MessageCreatorRequirement : IAuthorizationRequirement { }

public class MessageCreatorHandler : AuthorizationHandler<MessageCreatorRequirement>
{
    protected override Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        MessageCreatorRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;
        
        if (!httpContext.Request.RouteValues.TryGetValue("messageId", out var m))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'messageId'.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        string userId = context.User.FindFirstValue("id")!;
        string messageId = m!.ToString()!;

        if (messageId.Split("#")[1] != userId)
        {
            var reason = new AuthorizationFailureReason(this, $"User {userId} is not the creator of Message {messageId}.");
            context.Fail(reason);
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
