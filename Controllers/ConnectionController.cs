using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("connection")]
public class ConnectionController
(
    ChatConnectionService chatConnectionService,
    FollowService followService,
    StatusConnectionService statusConnectionService,
    StatusJobService statusJobService,
    UserService userService
) : ControllerBase
{
    private readonly ChatConnectionService _chatConnectionService = chatConnectionService;
    private readonly FollowService _followService = followService;
    private readonly StatusConnectionService _statusConnectionService = statusConnectionService;
    private readonly StatusJobService _statusJobService = statusJobService;
    private readonly UserService _userService = userService;

    [HttpPost("chat")]
    public async void CreateChatConnection([FromBody] ChatConnectionCreate connection)
    {
        Console.WriteLine("Adding Connection " + connection.Id);
        await _chatConnectionService.AddConnectionId(connection.ChatId, connection.Id);
    }

    [HttpDelete("chat")]
    public async void DeleteChatConnection([FromBody] ChatConnectionCreate connection)
    {
        Console.WriteLine("Deleting Connection " + connection.Id);
        await _chatConnectionService.RemoveConnectionId(connection.ChatId, connection.Id);
    }

    [HttpPost("status")]
    public async void CreateStatusConnection([FromBody] StatusConnectionCreate connection)
    {
        Console.WriteLine("Connected");
        Console.WriteLine("Id: " + connection.Id);
        Console.WriteLine("UserId: " + connection.UserId);

        // Load the user
        var loadUser = await _userService.Load(connection.UserId);            
        if (loadUser.Data is null || loadUser.IsFailure)
            return;

        User user = loadUser.Data;

        // Load everyone the User follows
        var loadAllFollowing = await _followService.LoadAllFollowing(user.Id);
        if (loadAllFollowing.Data is null || loadAllFollowing.IsFailure)
            return;
        
        List<string> followees = loadAllFollowing.Data.Select(f => f.Followee).ToList();

        // Get those who follow back
        var getMutuals = await _followService.GetMutuals(user.Id, followees);
        if (getMutuals.Data is null || getMutuals.IsFailure)
            return;

        List<string> friends = getMutuals.Data.Select(f => f.Follower).ToList();
        
        // Add the Connection Id to each friends' Status Connection list
        var add = await _statusConnectionService.AddConnectionId(friends, connection.Id);
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
