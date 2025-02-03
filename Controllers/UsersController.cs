using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Controllers;

[ApiController]
[Route("users")]
public class UsersController
(
    ChatsService chatsService,
    FeedsService feedsService,
    FollowersService followersService,
    PostsService postsService,
    StatusesService statusesService,
    UsersService usersService
) : ControllerBase
{
    private readonly ChatsService _chatsService = chatsService;
    private readonly FeedsService _feedsService = feedsService;
    private readonly FollowersService _followersService = followersService;
    private readonly PostsService _postsService = postsService;
    private readonly StatusesService _statusesService = statusesService;
    private readonly UsersService _usersService = usersService;
    
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        var load = await _usersService.Load(userId);

        if (load.IsFailure)
            return StatusCode(500);

        if (load.Data is null)
            return NotFound();

        return Ok(load.Data);
    }

    [Authorize(Self)]
    [HttpPut("{userId}")]
    public async Task<IActionResult> Put(string userId, [FromBody] UserUpdate dto)
    {
        var update = await _usersService.Update(userId, dto);

        if (update.IsFailure || update.Data is null)
            return StatusCode(500);
        
        return Ok(update.Data);
    }

    [Authorize(Self)]
    [HttpGet("{userId}/chats")]
    public async Task<IActionResult> GetChats(string userId, [FromQuery] int page = 1)
    {
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound();
        
        List<string> chatIds = loadUser.Data.ChatIds.ToList();

        var getChats = await _chatsService.LoadPage(chatIds, page);

        if (getChats.IsFailure)
            return StatusCode(500);
        
        var data = new 
        {
            chats = getChats.Data,
            page = page + 1
        };

        return Ok(data);
    }

    [Authorize(Self)]
    [HttpGet("{userId}/feed")]
    public async Task<IActionResult> GetFeed(string userId, [FromBody] string? exclusiveStartKey = null)
    {
        var load = await _feedsService.LoadPage(userId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            feed = load.Data.Item1,
            exclusiveStartKey = load.Data.Item2
        };

        return Ok(data);
    }

    [HttpGet("{userId}/followers")]
    public async Task<IActionResult> GetFollowers(string userId, [FromQuery] string? exclusiveStartKey = null)
    {
        // Ensure User exists
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound();
        
        // Load a page of the users that follow them
        var loadFollowers = await _followersService.LoadPageFollowers(userId, exclusiveStartKey);

        if (loadFollowers.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            followers = loadFollowers.Data.Item1,
            exclusiveStartKey = loadFollowers.Data.Item2
        };
        
        return Ok(data);
    }

    [Authorize(Self)]
    [HttpPost("{userId}/followers/{followerUserId}")]
    public async Task<IActionResult> CreateFollower(string userId, string followerUserId)
    {
        var follower = new Follower
        {
            UserId = userId,
            FollowerUserId = followerUserId,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _followersService.Save(follower);

        if (save.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [Authorize(Self)]
    [HttpDelete("{userId}/followers/{followerUserId}")]
    public async Task<IActionResult> DeleteFollower(string userId, string followerUserId)
    {
        var delete = await _followersService.Delete(userId, followerUserId);

        if (delete.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpGet("{userId}/followings")]
    public async Task<IActionResult> GetFollowings(string userId, [FromQuery] string? exclusiveStartKey = null)
    {
        // Ensure User exists
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound();

        // Load a page of the users they follow
        var loadFollowings = await _followersService.LoadPageFollowings(userId, exclusiveStartKey);

        if (loadFollowings.IsFailure)
            return StatusCode(500);

        return Ok(loadFollowings.Data.Item1);
    }

    [HttpGet("{userId}/posts")]
    public async Task<IActionResult> GetPosts(string userId, [FromQuery] string? exclusiveStartKey = null)
    {
        var load = await _postsService.LoadPage(userId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            posts = load.Data.Item1,
            exclusiveStartKey = load.Data.Item2
        };

        return Ok(data);
    }

    [HttpGet("{userId}/status")]
    public async Task<IActionResult> GetStatus(string userId)
    {
        var status = await _statusesService.Load(userId);

        if (status.IsFailure)
            return StatusCode(500);

        if (status.Data is null)
            return NotFound();

        return Ok(status.Data);
    }
}
