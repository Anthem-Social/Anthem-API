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
    UserService userService
): ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly FollowService _followService = followService;
    private readonly UserService _userService = userService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var load = await _userService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);

        if (load.Data is null)
            return NotFound();

        return Ok(load.Data);
    }

    [HttpPost("{follower}/follow/{followee}")]
    public async Task<IActionResult> Follow(string follower, string followee)
    {
        var follow = new Follow
        {
            Followee = followee,
            Follower = follower,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _followService.Save(follow);

        if (save.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpGet("{id}/chats")]
    public async Task<IActionResult> GetChats(string id, [FromQuery] int page = 1)
    {
        var loadUser = await _userService.Load(id);

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
}
