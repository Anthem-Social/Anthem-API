using System.Security.Claims;
using AnthemAPI.Common;
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
    
    [Authorize(AuthenticationSchemes = Spotify)]
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId)
    {
        // Load the User
        var load = await _usersService.Load(userId);

        if (load.IsFailure)
            return StatusCode(500);

        if (load.Data is null)
            return NotFound();
        
        string callerUserId = User.FindFirstValue("user_id")!;

        // Load the Relationship
        // User A is the one calling, User B is the one they are calling for
        var loadRelationship = await _followersService.LoadRelationship(callerUserId, userId);

        if (loadRelationship.IsFailure)
            return StatusCode(500);
        
        Relationship relationship = loadRelationship.Data;
        
        var data = new
        {
            relationship,
            user = load.Data
        };

        return Ok(data);
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
    [HttpGet("{userId}/claims")]
    public IActionResult GetClaims(string userId)
    {

        var claims = new Claims
        {
            AccessToken = User.FindFirstValue("access_token")!,
            Country = User.FindFirstValue("country")!,
            ExplicitContent = bool.Parse(User.FindFirstValue("explicit_content")!),
            Premium = bool.Parse(User.FindFirstValue("premium")!),
            UserId = userId
        };

        return Ok(claims);
    }

    [Authorize(Self)]
    [HttpGet("{userId}/feed")]
    public async Task<IActionResult> GetFeed(string userId, [FromBody] string? exclusiveStartKey = null)
    {
        // Load a page of the Feed
        var loadFeed = await _feedsService.LoadPage(userId, exclusiveStartKey);

        if (loadFeed.IsFailure)
            return StatusCode(500);

        List<Feed> feeds = loadFeed.Data.Item1;
        string? lastEvaluatedKey = loadFeed.Data.Item2;
        List<string> postIds = feeds.Select(feed => feed.PostId).ToList();
        
        // Load the Posts
        var loadPosts = await _postsService.Load(postIds);

        if (loadPosts.IsFailure)
            return StatusCode(500);
        
        List<Post> posts = loadPosts.Data!;
        HashSet<string> userIds = posts.Select(post => post.UserId).ToHashSet();

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create lookup dictionary
        Dictionary<string, Card> dict = cards.ToDictionary(card => card.UserId);

        // Create list of PostCard DTOs
        List<PostCard> postCards = posts
            .Select(post => new PostCard
            {
                Card = dict[post.UserId],
                Post = post
            })
            .ToList();

        // Create data to return
        var data = new
        {
            lastEvaluatedKey,
            posts = postCards
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
        
        // Load a page of Followers
        var loadFollowers = await _followersService.LoadPageFollowers(userId, exclusiveStartKey);

        if (loadFollowers.IsFailure)
            return StatusCode(500);
        
        List<Follower> followers = loadFollowers.Data.Item1;
        string? lastEvaluatedKey = loadFollowers.Data.Item2;
        HashSet<string> userIds = followers.Select(follower => follower.FollowerUserId).ToHashSet();

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create lookup dictionary
        Dictionary<string, Card> dict = cards.ToDictionary(card => card.UserId);

        // Create list of FollowerCards
        List<FollowerCard> followerCards = followers
            .Select(follower => new FollowerCard
            {
                Card = dict[follower.FollowerUserId],
                Follower = follower
            })
            .ToList();

        // Create data to return
        var data = new
        {
            followers = followerCards,
            lastEvaluatedKey
        };
        
        return Ok(data);
    }

    [Authorize(Self)]
    [HttpPost("{userId}/follow/{followeeUserId}")]
    public async Task<IActionResult> Follow(string userId, string followeeUserId)
    {
        var follower = new Follower
        {
            UserId = followeeUserId,
            FollowerUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _followersService.Save(follower);

        if (save.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [Authorize(Self)]
    [HttpDelete("{userId}/follow/{followeeUserId}")]
    public async Task<IActionResult> Unfollow(string userId, string followeeUserId)
    {
        var delete = await _followersService.Delete(followeeUserId, userId);

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

        // Load a page of Followings
        var loadFollowings = await _followersService.LoadPageFollowings(userId, exclusiveStartKey);

        if (loadFollowings.IsFailure)
            return StatusCode(500);

        List<Follower> followings = loadFollowings.Data.Item1;
        string? lastEvaluatedKey = loadFollowings.Data.Item2;
        HashSet<string> userIds = followings.Select(follower => follower.UserId).ToHashSet();

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create lookup dictionary
        Dictionary<string, Card> dict = cards.ToDictionary(card => card.UserId);

        // Create list of FollowerCard
        List<FollowerCard> followerCards = followings
            .Select(follower => new FollowerCard
            {
                Card = dict[follower.UserId],
                Follower = follower
            })
            .ToList();

        // Create data to return
        var data = new
        {
            followings = followerCards,
            lastEvaluatedKey
        };
        
        return Ok(data);
    }

    [HttpGet("{userId}/posts")]
    public async Task<IActionResult> GetPosts(string userId, [FromQuery] string? exclusiveStartKey = null)
    {
        // Load the User
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);
        
        if (loadUser.Data is null)
            return NotFound();
        
        User user = loadUser.Data;

        // Create the Card
        var card = new Card
        {
            UserId = user.Id,
            Nickname = user.Nickname,
            PictureUrl = user.PictureUrl
        };

        // Load a page of the Posts
        var loadPosts = await _postsService.LoadPage(userId, exclusiveStartKey);

        if (loadPosts.IsFailure)
            return StatusCode(500);

        List<Post> posts = loadPosts.Data.Item1;
        string? lastEvaluatedKey = loadPosts.Data.Item2;
        
        // Create list of PostCard DTOs
        List<PostCard> postCards = posts
            .Select(post => new PostCard
            {
                Card = card,
                Post = post
            })
            .ToList();
        
        // Create the data to return
        var data = new
        {
            lastEvaluatedKey,
            postCards
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
