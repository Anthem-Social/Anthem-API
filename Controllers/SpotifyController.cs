using System.Security.Claims;
using System.Text.Json;
using AnthemAPI.Common;
using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("spotify")]
public class SpotifyController
(
    AuthorizationsService authorizationsService,
    FollowersService followersService,
    SpotifyService spotifyService,
    TokenService tokenService,
    UsersService usersService
) : ControllerBase
{
    private readonly AuthorizationsService _authorizationsService = authorizationsService;
    private readonly FollowersService _followersService = followersService;
    private readonly SpotifyService _spotifyService = spotifyService;
    private readonly TokenService _tokenService = tokenService;
    private readonly UsersService _usersService = usersService;

    [HttpPut("anthem-queue-playlist")]
    public async Task<IActionResult> UpdateAnthemQueuePlaylist([FromBody] List<string> trackUris)
    {
        string accessToken = User.FindFirstValue("access_token")!;
        string userId = User.FindFirstValue("user_id")!;

        var get = await _spotifyService.GetAnthemQueuePlaylistId(accessToken, userId);

        if (get.IsFailure || get.Data is null)
            return StatusCode(500);

        string playlistId = get.Data;

        var update = await _spotifyService.UpdateAnthemQueuePlaylist(accessToken, playlistId, trackUris);

        if (update.IsFailure)
            return StatusCode(500);

        return Ok(playlistId);
    }

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

        // Get the user's account information
        var get = await _spotifyService.GetAccount(accessToken);

        if (get.Data is null || get.IsFailure)
            return StatusCode(500);

        string userId = get.Data.Id;
                
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
        // Swap for an access token
        var swap = await _tokenService.Swap(code);

        if (swap.Data is null || swap.IsFailure)
            return StatusCode(500);

        JsonElement json = JsonDocument.Parse(swap.Data!).RootElement;
        string accessToken = json.GetProperty("access_token").GetString()!;

        // Get the user's account information
        var get = await _spotifyService.GetAccount(accessToken);

        if (get.Data is null || get.IsFailure)
            return StatusCode(500);

        Account account = get.Data;

        // Update the user's account information
        var update = await _usersService.UpdateAccountInformation(account);

        if (update.IsFailure)
            return StatusCode(500);
        
        // Save the user's tokens for Status job
        var save = await _authorizationsService.Save(account.Id, json);

        if (save.Data is null || save.IsFailure)
            return StatusCode(500);

        // Ensure this user has a mutual following with all other users
        // Load followers
        var loadFollowers = await _followersService.LoadAllFollowers(account.Id);

        if (loadFollowers.IsFailure)
            return StatusCode(500);
        
        List<string> followerUserIds = loadFollowers.Data!.Select(f => f.FollowerUserId).ToList();
        
        // Load followings
        var loadFollowings = await _followersService.LoadAllFollowings(account.Id);

        if (loadFollowings.IsFailure)
            return StatusCode(500);
        
        List<string> followingUserIds = loadFollowings.Data!.Select(f => f.UserId).ToList();

        // Get all user ids
        var getAllUserIds = await _usersService.GetAllUserIds();

        if (getAllUserIds.IsFailure)
            return StatusCode(500);

        List<string> allUserIds = getAllUserIds.Data!;

        // Get missing followers and followings
        List<string> missingFollowers = allUserIds.Except(followerUserIds).ToList();
        List<string> missingFollowings = allUserIds.Except(followingUserIds).ToList();

        // Add missing followers
        foreach (string missingFollower in missingFollowers)
        {
            var follower = new Follower
            {
                UserId = account.Id,
                FollowerUserId = missingFollower,
                CreatedAt = DateTime.UtcNow,
            };

            var saveFollower = await _followersService.Save(follower);

            if (saveFollower.IsFailure)
                return StatusCode(500);
        }

        // Add missing followings
        foreach (string missingFollowing in missingFollowings)
        {
            var following = new Follower
            {
                UserId = missingFollowing,
                FollowerUserId = account.Id,
                CreatedAt = DateTime.UtcNow,
            };

            var saveFollowing = await _followersService.Save(following);

            if (saveFollowing.IsFailure)
                return StatusCode(500);
        }

        return Ok(swap.Data);
    }
}
