using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("connection")]
public class ConnectionController
(
    ChatConnectionService chatConnectionService,
    FollowersService followersService,
    StatusConnectionService statusConnectionService,
    StatusJobService statusJobService,
    UsersService usersService
) : ControllerBase
{
    private readonly ChatConnectionService _chatConnectionService = chatConnectionService;
    private readonly FollowersService _followersService = followersService;
    private readonly StatusConnectionService _statusConnectionService = statusConnectionService;
    private readonly StatusJobService _statusJobService = statusJobService;
    private readonly UsersService _usersService = usersService;

    [HttpPost("chat")]
    public async void CreateChatConnection([FromBody] ChatConnectionCreate connection)
    {
        Console.WriteLine("Connected.");
        Console.WriteLine("ConnectionId: " + connection.ConnectionId);
        Console.WriteLine("ChatId: " + connection.ChatId);
        await _chatConnectionService.AddConnectionId(connection.ChatId, connection.ConnectionId);
    }

    [HttpPost("status")]
    public async void CreateStatusConnection([FromBody] StatusConnectionCreate connection)
    {
        Console.WriteLine("Connected.");
        Console.WriteLine("ConnectionId: " + connection.ConnectionId);
        Console.WriteLine("UserId: " + connection.UserId);

        // Load the user
        var loadUser = await _usersService.Load(connection.UserId);            
        if (loadUser.Data is null || loadUser.IsFailure)
            return;

        User user = loadUser.Data;

        // Load everyone the User follows
        var loadAllFollowing = await _followersService.LoadAllFollowings(user.Id);
        if (loadAllFollowing.Data is null || loadAllFollowing.IsFailure)
            return;
        
        List<string> followees = loadAllFollowing.Data.Select(f => f.UserId).ToList();

        // Get those who follow back
        var loadFriends = await _followersService.LoadFriends(user.Id, followees);
        if (loadFriends.Data is null || loadFriends.IsFailure)
            return;

        List<string> friends = loadFriends.Data.Select(f => f.FollowerUserId).ToList();
        
        // Add the Connection Id to each friends' Status Connection list
        var add = await _statusConnectionService.AddConnectionId(friends, connection.ConnectionId);
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
