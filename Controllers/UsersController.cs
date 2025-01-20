using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("users")]
public class UsersController
(
    ChatService chatService,
    FollowService followService,
    StatusConnectionService statusConnectionService,
    StatusJobService statusJobService,
    StatusService statusService,
    UserService userService
): ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly FollowService _followService = followService;
    private readonly StatusConnectionService _statusConnectionService = statusConnectionService;
    private readonly StatusJobService _statusJobService = statusJobService;
    private readonly StatusService _statusService = statusService;
    private readonly UserService _userService = userService;

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string id)
    {
        var load = await _userService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);

        if (load.Data is null)
            return NotFound();

        return Ok(load.Data);
    }

    [HttpPost("{userId}/follower/{followerUserId}")]
    public async Task<IActionResult> Follow(string userId, string followerUserId)
    {
        var follow = new Follow
        {
            Followee = userId,
            Follower = followerUserId,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _followService.Save(follow);

        if (save.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpGet("{userId}/chats")]
    public async Task<IActionResult> GetChats(string userId, [FromQuery] int page = 1)
    {
        var loadUser = await _userService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound();
        
        List<string> chatIds = loadUser.Data.ChatIds.ToList();

        var getChats = await _chatService.GetPage(chatIds, page);

        if (getChats.IsFailure)
            return StatusCode(500);

        return Ok(getChats.Data);
    }

    // [HttpGet("{userId}/posts")]

    [HttpGet("{userId}/status")]
    public async Task<IActionResult> GetStatus(string userId)
    {
        var status = await _statusService.Load(userId);

        if (status.IsFailure)
            return StatusCode(500);

        if (status.Data is null)
            return NotFound();

        return Ok(status.Data);
    }
}
