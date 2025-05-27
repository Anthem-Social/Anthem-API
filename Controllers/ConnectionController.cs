using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("connection")]
public class ConnectionController
(
    ChatConnectionsService chatConnectionsService,
    FollowersService followersService,
    StatusConnectionsService statusConnectionsService,
    StatusJobService statusJobService,
    UsersService usersService
) : ControllerBase
{
    private readonly ChatConnectionsService _chatConnectionsService = chatConnectionsService;
    private readonly FollowersService _followersService = followersService;
    private readonly StatusConnectionsService _statusConnectionsService = statusConnectionsService;
    private readonly StatusJobService _statusJobService = statusJobService;
    private readonly UsersService _usersService = usersService;

    [AllowAnonymous]
    [HttpPost("chat")]
    public async void CreateChatConnection([FromBody] ChatConnectionCreate dto)
    {
        Console.WriteLine($"Connected to chat {dto.ChatId}.");
        await _chatConnectionsService.AddConnection(dto.ChatId, dto.ConnectionId);
    }
    
    [AllowAnonymous]
    [HttpPost("status")]
    public async void CreateStatusConnection([FromBody] StatusConnectionCreate dto)
    {
        Console.WriteLine($"{dto.UserId} connected to status updates.");

        // Load the user
        var loadUser = await _usersService.Load(dto.UserId);            
        if (loadUser.Data is null || loadUser.IsFailure)
            return;

        User user = loadUser.Data;

        var loadFriends = await _followersService.LoadFriends(user.Id);
        if (loadFriends.Data is null || loadFriends.IsFailure)
            return;

        List<string> friends = loadFriends.Data.Select(f => f.FollowerUserId).ToList();
        
        // Add the Connection Id to each friends' Status Connection list
        var add = await _statusConnectionsService.AddConnectionToAll(friends, dto.ConnectionId);
        if (add.IsFailure)
            return;

        // Schedule each job if not already scheduled
        foreach (var friend in friends)
        {
            var exists = await _statusJobService.Exists(friend);
            if (exists.IsFailure)
                continue;

            if (!exists.Data)
            {
                await _statusJobService.Schedule(friend);
            }
        }
    }
}
