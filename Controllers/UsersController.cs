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
    AuthorizationsService authorizationsService,
    ChatsService chatsService,
    FeedsService feedsService,
    FollowersService followersService,
    LikesService likesService,
    PostsService postsService,
    StatusConnectionsService statusConnectionsService,
    StatusesService statusesService,
    UsersService usersService
) : ControllerBase
{
    private readonly AuthorizationsService _authorizationsService = authorizationsService;
    private readonly ChatsService _chatsService = chatsService;
    private readonly FeedsService _feedsService = feedsService;
    private readonly FollowersService _followersService = followersService;
    private readonly LikesService _likesService = likesService;
    private readonly PostsService _postsService = postsService;
    private readonly StatusConnectionsService _statusConnectionsService = statusConnectionsService;
    private readonly StatusesService _statusesService = statusesService;
    private readonly UsersService _usersService = usersService;

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
    [HttpDelete("{userId}/account")]
    public async Task<IActionResult> Delete(string userId)
    {
        // Delete Authorization
        await _authorizationsService.Delete(userId);

        // Delete Feed
        await _feedsService.Delete(userId);

        // Decrement TotalFollowings for each Follower
        var loadFollowers = await _followersService.LoadAllFollowers(userId);
        List<string> followerIds = loadFollowers.Data!.Select(f => f.FollowerUserId).ToList();
        foreach (string followerId in followerIds)
        {
            await _usersService.DecrementTotalFollowings(followerId);
        }

        // Delete Followers
        await _followersService.DeleteAllFollowers(userId);

        // Decrement TotalFollowers for each Following
        var loadFollowings = await _followersService.LoadAllFollowings(userId);
        List<string> followingsIds = loadFollowings.Data!.Select(f => f.UserId).ToList();
        foreach (string followingsId in followingsIds)
        {
            await _usersService.DecrementTotalFollowers(followingsId);
        }

        // Delete Followings
        await _followersService.DeleteAllFollowings(userId);

        // Delete all of the Likes they have given
        await _likesService.DeleteAllByUserId(userId);

        // Delete all of the Likes on their Posts
        var loadPosts = await _postsService.LoadAll(userId);
        List<string> postIds = loadPosts.Data!.Select(p => p.Id).ToList();
        foreach (string postId in postIds)
        {
            await _likesService.DeleteAllByPostId(postId);
        }

        // Delete Posts
        await _postsService.DeleteAll(userId);

        // Delete Status
        await _statusesService.Delete(userId);

        // Delete StatusConnections
        await _statusConnectionsService.Delete(userId);

        // Delete User
        await _usersService.Delete(userId);

        return NoContent();
    }
    
    [Authorize(Self)]
    [HttpGet("{userId}/chats")]
    public async Task<IActionResult> GetChats(string userId, [FromQuery] int page = 1)
    {
        // var loadUser = await _usersService.Load(userId);

        // if (loadUser.IsFailure)
        //     return StatusCode(500);

        // if (loadUser.Data is null)
        //     return NotFound();
        
        // List<string> chatIds = loadUser.Data.ChatIds.ToList();

        // var getChats = await _chatsService.LoadPage(chatIds, page);

        // if (getChats.IsFailure)
        //     return StatusCode(500);
        
        // var data = new 
        // {
        //     chats = getChats.Data,
        //     page = page + 1
        // };

        // return Ok(data);

        await Task.Delay(0);
        return StatusCode(501);
    }

    [HttpGet("claims")]
    public IActionResult GetClaims()
    {
        var claims = new Claims
        {
            Country = User.FindFirstValue("country")!,
            ExplicitContent = bool.Parse(User.FindFirstValue("explicit_content")!),
            Premium = bool.Parse(User.FindFirstValue("premium")!),
            UserId = User.FindFirstValue("user_id")!
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
        var loadPosts = await _postsService.LoadFromList(postIds);

        if (loadPosts.IsFailure)
            return StatusCode(500);
        
        List<Post> posts = loadPosts.Data!;
        HashSet<string> userIds = posts.Select(post => post.UserId).ToHashSet();

        // Check if the User has liked any of these Posts
        var getLikes = await _likesService.Load(postIds, userId);

        if (getLikes.IsFailure)
            return StatusCode(500);
        
        List<Like?> likes = getLikes.Data!;

        // Create Like lookup by PostId
        Dictionary<string, Like?> likesDict = likes.ToDictionary(like => like!.PostId);

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create Card lookup by UserId
        Dictionary<string, Card> cardsDict = cards.ToDictionary(card => card.UserId);

        // Create list of PostCards
        List<PostCard> postCards = posts
            .Select(post => new PostCard
            {
                Card = cardsDict[post.UserId],
                Like = likesDict.TryGetValue(post.Id, out var like) ? like : null,
                Post = post
            })
            .OrderByDescending(postCard => postCard.Post.Id)
            .ToList();
        
        // Create data to return
        var data = new
        {
            lastEvaluatedKey,
            posts = postCards
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
            results = followerCards,
            lastEvaluatedKey
        };
        
        return Ok(data);
    }

    [Authorize(Self)]
    [HttpPost("{followeeUserId}/followers/{userId}")]
    public async Task<IActionResult> Follow(string userId, string followeeUserId)
    {
        // Create Follower
        var follower = new Follower
        {
            UserId = followeeUserId,
            FollowerUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var saveFollower = await _followersService.Save(follower);

        if (saveFollower.IsFailure)
            return StatusCode(500);
        
        // Increment followee's TotalFollowers
        var incrementFollowers = await _usersService.IncrementTotalFollowers(followeeUserId);

        if (incrementFollowers.IsFailure)
            return StatusCode(500);

        // Increment follower's TotalFollowings
        var incrementFollowings = await _usersService.IncrementTotalFollowings(userId);

        if (incrementFollowings.IsFailure)
            return StatusCode(500);

        return Created();
    }

    [Authorize(Self)]
    [HttpDelete("{followeeUserId}/followers/{userId}")]
    public async Task<IActionResult> Unfollow(string userId, string followeeUserId)
    {
        // Delete Follower
        var deleteFollower = await _followersService.Delete(followeeUserId, userId);

        if (deleteFollower.IsFailure)
            return StatusCode(500);
        
        // Decrement followee's TotalFollowers
        var decrementFollowers = await _usersService.DecrementTotalFollowers(followeeUserId);

        if (decrementFollowers.IsFailure)
            return StatusCode(500);

        // Decrement follower's TotalFollowings
        var decrementFollowings = await _usersService.DecrementTotalFollowings(userId);

        if (decrementFollowings.IsFailure)
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
            results = followerCards,
            lastEvaluatedKey
        };
        
        return Ok(data);
    }

    [HttpGet("{userId}/profile")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        // Load the User
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);
        
        if (loadUser.Data is null)
            return NotFound();

        // Load the Relationship, User A is the one calling, User B is the one they are calling for
        var loadRelationship = await _followersService.LoadRelationship(User.FindFirstValue("user_id")!, userId);

        if (loadRelationship.IsFailure)
            return StatusCode(500);
                
        // Load the Status
        var loadStatus = await _statusesService.Load(userId);

        if (loadStatus.IsFailure)
            return StatusCode(500);
        
        // Load the Posts
        var loadPosts = await _postsService.LoadPage(userId);

        if (loadPosts.IsFailure)
            return StatusCode(500);

        List<Post> posts = loadPosts.Data.Item1;
        List<string> postIds = posts.Select(post => post.Id).ToList();
        HashSet<string> userIds = posts.Select(post => post.UserId).ToHashSet();

        // Check if the User has liked any of these Posts
        var loadLikes = await _likesService.Load(postIds, userId);

        if (loadLikes.IsFailure)
            return StatusCode(500);
        
        List<Like?> likes = loadLikes.Data!;

        // Create Like lookup by PostId
        Dictionary<string, Like?> likesDict = likes.ToDictionary(like => like!.PostId);

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create Card lookup by UserId
        Dictionary<string, Card> cardsDict = cards.ToDictionary(card => card.UserId);

        // Create list of PostCards
        List<PostCard> postCards = posts
            .Select(post => new PostCard
            {
                Card = cardsDict[post.UserId],
                Like = likesDict.TryGetValue(post.Id, out var like) ? like : null,
                Post = post
            })
            .OrderByDescending(postCard => postCard.Post.Id)
            .ToList();

        // Return the Data
        var data = new
        {
            lastEvaluatedKey = loadPosts.Data.Item2,
            postCards,
            relationship = loadRelationship.Data,
            status = loadStatus.Data,
            user = loadUser.Data
        };

        return Ok(data);
    }

    [Authorize(Self)]
    [HttpGet("{userId}/statuses")]
    public async Task<IActionResult> GetStatuses(string userId)
    {
        // Load Friends
        var loadFriends = await _followersService.LoadFriends(userId);

        if (loadFriends.IsFailure)
            return StatusCode(500);

        HashSet<string> userIds = loadFriends.Data!.Select(f => f.FollowerUserId).ToHashSet();

        if (userIds.Count == 0)
        {
            return Ok(new
            {
                statusCards = new List<StatusCard>()
            });
        }
        
        // Get their Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create Card lookup by UserId
        Dictionary<string, Card> cardsDict = cards.ToDictionary(card => card.UserId);

        // Load their Statuses
        var loadStatuses = await _statusesService.LoadAll(userIds.ToList());

        if (loadStatuses.IsFailure || loadStatuses.Data is null)
            return StatusCode(500);
        
        List<Status> statuses = loadStatuses.Data;

        // Create list of StatusCards
        List<StatusCard> statusCards = statuses
            .Select(status => new StatusCard
            {
                UserId = status.UserId,
                Card = cardsDict[status.UserId],
                Status = status
            })
            .OrderByDescending(statusCards => statusCards.Status.LastChanged)
            .ToList();
        
        var data = new
        {
            statusCards
        };

        return Ok(data);
    }
}
