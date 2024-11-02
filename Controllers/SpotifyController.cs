using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnthemAPI.Controllers;

[Route("spotify")]
[ApiController]
public class SpotifyController
(
    SpotifyService spotifyService
): ControllerBase
{
    private readonly SpotifyService _spotifyService = spotifyService;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // var result = await _spotifyService.GetMe();
        // return Ok(result.Data);
        var me = new Me
        {
            UserId = "schreineravery-us",
            IsPremium = true,
            Country = "US"
        };
        return Ok(me);
    }

    [HttpGet("user")]
    public async Task<IActionResult> User()
    {
        var user = new User {
            UserId = "schreineravery-us",
            Alias = "Ave",
            PictureUrl = "https://picture.com",
            LastActive = DateTime.Now,
            LastTrack = new Resource {
                Type = Common.ResourceType.Track,
                Uri = "spotify:track:6wwruKU956ldDyz55DZTt1"
            }
        };

        return Ok(user);
    }
}
