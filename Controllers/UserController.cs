using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("user")]
public class UserController
(
    UserService userService
): ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _userService.Load(id);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return StatusCode(500);
    }

    [HttpPost("{id}/follower/{userId}")]
    public async Task<IActionResult> AddFollower(string id, string userId)
    {
        var result = await _userService.AddFollower(id, userId);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return StatusCode(500);
    }

    [HttpPost("{id}/following/{userId}")]
    public async Task<IActionResult> AddFollowing(string id, string userId)
    {
        var result = await _userService.AddFollowing(id, userId);

        if (result.IsSuccess)
        {
            return Ok();
        }

        return StatusCode(500);
    }
}
