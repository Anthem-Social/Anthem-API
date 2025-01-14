using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("users")]
public class UsersController
(
    ChatService chatService,
    UserService userService
): ControllerBase
{
    private readonly ChatService _chatService = chatService;
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

    [HttpPost("{id}/follower/{followerId}")]
    public async Task<IActionResult> Follow(string id, string followerId)
    {
        return Ok();
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
