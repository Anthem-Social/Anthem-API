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

    [HttpPut("save/album/{id}")]
    public async Task<IActionResult> SaveAlbum(string id)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var save = await _spotifyService.SaveTrack(accessToken, id);

        if (save.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpDelete("save/album/{id}")]
    public async Task<IActionResult> UnsaveAlbum(string id)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var unsave = await _spotifyService.UnsaveTrack(accessToken, id);

        if (unsave.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpPut("save/track/{id}")]
    public async Task<IActionResult> SaveTrack(string id)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var save = await _spotifyService.SaveTrack(accessToken, id);

        if (save.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpDelete("save/track/{id}")]
    public async Task<IActionResult> UnsaveTrack(string id)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        var unsave = await _spotifyService.UnsaveTrack(accessToken, id);

        if (unsave.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpGet("search/albums")]
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

    [HttpGet("search/artists")]
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

    [HttpGet("search/tracks")]
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

    [AllowAnonymous]
    [HttpPost("token/refresh")]
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
    [HttpPost("token/swap")]
    public async Task<IActionResult> Swap([FromForm] string code)
    {
        Console.WriteLine("In Swap with code: " + code);
        // Swap for an access token
        var swap = await _tokenService.Swap(code);

        if (swap.Data is null || swap.IsFailure)
            return StatusCode(500);

        JsonElement json = JsonDocument.Parse(swap.Data!).RootElement;
        string accessToken = json.GetProperty("access_token").GetString()!;

        Console.WriteLine("In Swap, got access token: " + accessToken);

        // Get the user's subscription level
        var get = await _spotifyService.GetSubscriptionLevel(accessToken);

        if (get is null || get.IsFailure)
            return StatusCode(500);
        
        string userId = get.Data.Item1;
        MusicProvider musicProvider = get.Data.Item2;

        Console.WriteLine("In Swap, got userId: " + userId + " and musicProvider: " + musicProvider);

        // Update the user's music provider
        var update = await _usersService.UpdateMusicProvider(userId, musicProvider);

        if (update.IsFailure)
            return StatusCode(500);

        Console.WriteLine("In Swap, updated music provider successfully");
        
        // Save the user's tokens for Status job
        var save = await _authorizationsService.Save(userId, json);

        if (save.Data is null || save.IsFailure)
            return StatusCode(500);
        
        Console.WriteLine("In Swap, saved auth successfully");

        return Ok(swap.Data);
    }
}
