using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AnthemAPI.Requirements;

public class ChatCreatorRequirement : IAuthorizationRequirement { }

public class ChatCreatorHandler
(
    ChatsService chatsService
) : AuthorizationHandler<ChatCreatorRequirement>
{
    private readonly ChatsService _chatsService = chatsService;

    protected override async Task HandleRequirementAsync
    (
        AuthorizationHandlerContext context,
        ChatCreatorRequirement requirement
    )
    {
        HttpContext httpContext = (HttpContext) context.Resource!;
        
        if (!httpContext.Request.RouteValues.TryGetValue("chatId", out var c))
        {
            var reason = new AuthorizationFailureReason(this, "No route value for 'chatId'.");
            context.Fail(reason);
            return;
        }

        string userId = context.User.FindFirstValue("id")!;
        string chatId = c!.ToString()!;

        var result = await _chatsService.Load(chatId);

        if (result.IsFailure || result.Data is null)
        {
            var reason = new AuthorizationFailureReason(this, "Load failed or data is null.");
            context.Fail(reason);
            return;
        }

        Chat chat = result.Data;

        if (chat.CreatorUserId != userId)
        {
            var reason = new AuthorizationFailureReason(this, $"User {userId} is not the creator of chat {chatId}.");
            context.Fail(reason);
            return;
        }

        context.Succeed(requirement);
        return;
    }
}
