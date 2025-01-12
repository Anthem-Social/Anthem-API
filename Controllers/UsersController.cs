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

    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] Follow follow)
    {
        var followee = await _userService.AddFollower(follow.Followee, follow.Follower);

        if (followee.IsFailure)
            return StatusCode(500);

        var follower = await _userService.AddFollowing(follow.Follower, follow.Followee);

        if (follower.IsFailure)
            return StatusCode(500);

        return Ok(followee.Data);
    }
}
