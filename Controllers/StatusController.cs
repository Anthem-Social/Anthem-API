using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("status")]
public class StatusController
(
    JobService jobService,
    StatusConnectionService statusConnectionService,
    StatusService statusService,
    UserService userService
) : ControllerBase
{
    private readonly JobService _jobService = jobService;
    private readonly StatusConnectionService _statusConnectionService = statusConnectionService;
    private readonly StatusService _statusService = statusService;
    private readonly UserService _userService = userService;

    [HttpPost("connect")]
    public async void Connect([FromBody] Connection connection)
    {
        Console.WriteLine("Connected");
        Console.WriteLine("Id: " + connection.Id);
        Console.WriteLine("UserId: " + connection.UserId);

        // Get all of the user's friends' ids
        var friends = await _userService.Load(connection.UserId);            
        if (friends.Data is null || friends.IsFailure)
            return;

        // TODO: fetch from followService
        List<string> friendIds = friends.Data.ChatIds.ToList();

        // Add the user's connection id to each friends' status connection list
        var add = await _statusConnectionService.AddConnectionId(friendIds, connection.Id);
        if (add.IsFailure)
            return;

        // Schedule each job if not already scheduled
        foreach (var id in friendIds)
        {
            var exists = await _jobService.Exists(id);
            if (exists.IsFailure)
                continue;

            if (!exists.Data)
            {
                await _jobService.Schedule(id);
            }
        }
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        var status = await _statusService.Load(userId);

        if (status.IsFailure)
            return StatusCode(500);

        if (status.Data is null)
            return NotFound();

        return Ok(status.Data);
    }
}
