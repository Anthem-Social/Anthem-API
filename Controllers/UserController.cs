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
        var user = await _userService.Load(id);

        if (user.Data is null)
        {
            return NotFound(new {
                error = $"User '{id}' not found."
            });
        }

        if (user.IsFailure) return StatusCode(500);

        return Ok(user.Data);
    }

    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] Follow follow)
    {
        var follower = await _userService.AddFollower(follow.Followee, follow.Follower);

        if (follower.IsFailure) return StatusCode(500);

        var following = await _userService.AddFollowing(follow.Follower, follow.Followee);

        if (following.IsFailure) return StatusCode(500);

        return NoContent();
    }
}
