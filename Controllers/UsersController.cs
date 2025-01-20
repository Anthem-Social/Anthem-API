using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("users")]
public class UsersController
(
    ChatService chatService,
    FollowerService followerService,
    StatusService statusService,
    UserService userService
): ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly FollowerService _followerService = followerService;
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

    [HttpPost("{userId}/followers/{followerUserId}")]
    public async Task<IActionResult> CreateFollower(string userId, string followerUserId)
    {
        var follower = new Follower
        {
            UserId = userId,
            FollowerUserId = followerUserId,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _followerService.Save(follower);

        if (save.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpDelete("{userId}/followers/{followerUserId}")]
    public async Task<IActionResult> DeleteFollower(string userId, string followerUserId)
    {
        var delete = await _followerService.Delete(userId, followerUserId);

        if (delete.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpGet("{userId}/followers")]
    public async Task<IActionResult> GetFollowers(string userId, [FromQuery] int page = 1)
    {
        var load = await _followerService.LoadFollowers(userId, page);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();

        return Ok(load.Data);
    }

    [HttpGet("{userId}/following")]
    public async Task<IActionResult> GetFollowing(string userId, [FromQuery] int page = 1)
    {
        var load = await _followerService.LoadFollowing(userId, page);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();

        return Ok(load.Data);
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
