using System.Security.Cryptography;
using System.Text;
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
        var result = await _spotifyService.GetMe();
        var result1 = await _spotifyService.GetJake();
        return Ok(result);
    }
}
