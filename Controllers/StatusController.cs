using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("status")]
public class StatusController
(
    JobService jobService,
    StatusConnectionService statusConnectionService,
    UserService userService
) : ControllerBase
{
    private readonly JobService _jobService = jobService;
    private readonly StatusConnectionService _statusConnectionService = statusConnectionService;
    private readonly UserService _userService = userService;

    [HttpPost("connect")]
    public async void Connect([FromBody] Connection connection)
    {
        Console.WriteLine("Connected");
        Console.WriteLine("Id: " + connection.Id);
        Console.WriteLine("UserId: " + connection.UserId);

        // Get all of the user's friends' Ids
        var friendsResult = await _userService.Load(connection.UserId);
        if (friendsResult.Data is null || friendsResult.IsFailure) return;

        List<string> friendIds = friendsResult.Data.Friends.ToList();

        // Add the user's connection Id to each friends' list of status receivers
        var batchAddResult = await _statusConnectionService.AddConnectionId(friendIds, connection.Id);
        if (batchAddResult.IsFailure) return;

        // Schedule each job if not already scheduled
        foreach (var id in friendIds)
        {
            var existsResult = await _jobService.Exists(id);
            if (existsResult.IsFailure) continue;

            bool exists = existsResult.Data;

            if (!exists)
            {
                await _jobService.Schedule(id);
            }
        }
    }
}
