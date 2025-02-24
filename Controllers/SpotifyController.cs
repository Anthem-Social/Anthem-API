using System.Security.Claims;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("spotify")]
public class SpotifyController
(
    AuthorizationsService authorizationsService,
    SpotifyService spotifyService,
    TokenService tokenService,
    UsersService usersService
) : ControllerBase
{
    private readonly AuthorizationsService _authorizationsService = authorizationsService;
    private readonly SpotifyService _spotifyService = spotifyService;
    private readonly TokenService _tokenService = tokenService;
    private readonly UsersService _usersService = usersService;

    [HttpGet("albums")]
    public async Task<IActionResult> SearchAlbums([FromQuery] string query)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var search = await _spotifyService.SearchAlbums(accessToken, query);

        if (search.IsFailure)
            return StatusCode(500);
        
        if (search.Data is null)
            return NotFound();
        
        return Ok(search.Data);
    }

    [HttpGet("artists")]
    public async Task<IActionResult> SearchArtists([FromQuery] string query)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var search = await _spotifyService.SearchArtists(accessToken, query);

        if (search.IsFailure)
            return StatusCode(500);
        
        if (search.Data is null)
            return NotFound();
        
        return Ok(search.Data);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromForm] string refreshToken)
    {
        // Refresh the access token
        var refresh = await _tokenService.Refresh(refreshToken);

        if (refresh.Data is null || refresh.IsFailure)
            return StatusCode(500);

        string complete = Utility.AddRefreshTokenProperty(refresh.Data, refreshToken);
        JsonElement json = JsonDocument.Parse(complete).RootElement;
        string accessToken = json.GetProperty("access_token").GetString()!;

        // Get the user's subscription level
        var get = await _spotifyService.GetSubscriptionLevel(accessToken);

        if (get is null || get.IsFailure)
            return StatusCode(500);
        
        string userId = get.Data.Item1;
        
        // Save the new access token
        var save = await _authorizationsService.Save(userId, json);

        if (save.Data is null || save.IsFailure)
            return StatusCode(500);

        return Ok(refresh.Data);
    }

    [AllowAnonymous]
    [HttpPost("swap")]
    public async Task<IActionResult> Swap([FromForm] string code)
    {
        // Swap for an access token
        var swap = await _tokenService.Swap(code);

        if (swap.Data is null || swap.IsFailure)
            return StatusCode(500);

        JsonElement json = JsonDocument.Parse(swap.Data!).RootElement;
        string accessToken = json.GetProperty("access_token").GetString()!;

        // Get the user's subscription level
        var get = await _spotifyService.GetSubscriptionLevel(accessToken);

        if (get is null || get.IsFailure)
            return StatusCode(500);
        
        string userId = get.Data.Item1;
        MusicProvider musicProvider = get.Data.Item2;

        // Update the user's music provider
        var update = await _usersService.UpdateMusicProvider(userId, musicProvider);

        if (update.IsFailure)
            return StatusCode(500);
        
        // Save the user's tokens for Status job
        var save = await _authorizationsService.Save(userId, json);

        if (save.Data is null || save.IsFailure)
            return StatusCode(500);

        return Ok(swap.Data);
    }

    [HttpGet("tracks")]
    public async Task<IActionResult> SearchTracks([FromQuery] string query)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var search = await _spotifyService.SearchTracks(accessToken, query);

        if (search.IsFailure)
            return StatusCode(500);
        
        if (search.Data is null)
            return NotFound();
        
        return Ok(search.Data);
    }
}
