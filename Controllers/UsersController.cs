using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("users")]
public class UsersController
(
    UserService userService
): ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var user = await _userService.Load(id);

        if (user.IsFailure)
            return StatusCode(500);

        if (user.Data is null)
            return NotFound();

        return Ok(user.Data);
    }

    [HttpPost("{id}/follower/{followerId}")]
    public async Task<IActionResult> Follow(string id, string followerId)
    {
        return Ok();
    }
}
