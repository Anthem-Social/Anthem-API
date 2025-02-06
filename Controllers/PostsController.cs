using System.Security.Claims;
using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AnthemAPI.Common.Constants;

[ApiController]
[Route("posts")]
public class PostsController
(
    CommentsService commentsService,
    FeedsService feedsService,
    FollowersService followersService,
    LikesService likesService,
    PostsService postsService
) : ControllerBase
{
    private readonly CommentsService _commentsService = commentsService;
    private readonly FeedsService _feedsService = feedsService;
    private readonly FollowersService _followersService = followersService;
    private readonly LikesService _likesService = likesService;
    private readonly PostsService _postsService = postsService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostCreate dto)
    {
        string userId = User.FindFirstValue("user_id")!;

        // Save the new Post
        var post = new Post
        {
            UserId = userId,
            Id = $"{DateTime.UtcNow:o}#{userId}",
            ContentType = dto.ContentType,
            Content = dto.Content,
            Text = dto.Text,
            TotalLikes = 0,
            TotalComments = 0
        };

        var savePost = await _postsService.Save(post);

        if (savePost.IsFailure)
            return StatusCode(500);
        
        // Load the User's friends
        var loadFriends = await _followersService.LoadFriends(userId);

        if (loadFriends.IsFailure)
            return StatusCode(500);
        
        if (loadFriends.Data is not null && loadFriends.Data.Count > 0)
            return Created();
        
        List<string> userIds = loadFriends.Data!.Select(f => f.FollowerUserId).ToList();
        
        // Save the Post to all their Feeds
        var saveFeeds = await _feedsService.SaveAll(userIds, post.Id);

        if (saveFeeds.IsFailure)
            return StatusCode(500);

        return Created();
    }

    [HttpGet("{postId}")]
    public async Task<IActionResult> Get(string postId)
    {
        var load = await _postsService.Load(postId);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        return Ok(load.Data);
    }

    [Authorize(PostCreator)]
    [HttpDelete("{postId}")]
    public async Task<IActionResult> Delete(string postId)
    {
        string userId = User.FindFirstValue("user_id")!;

        // Delete the Post
        var delete = await _postsService.Delete(postId);

        if (delete.IsFailure)
            return StatusCode(500);

        // Load the User's friends
        var loadFriends = await _followersService.LoadFriends(userId);

        if (loadFriends.IsFailure)  
            return StatusCode(500);
        
        if (loadFriends.Data is not null && loadFriends.Data.Count > 0)
            return NoContent();
        
        List<string> userIds = loadFriends.Data!.Select(f => f.FollowerUserId).ToList();

        // Delete the Post from all their Feeds
        var deleteAll = await _feedsService.DeleteAll(userIds, postId);

        if (deleteAll.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] CommentCreate dto)
    {
        string userId = User.FindFirstValue("user_id")!;

        var comment = new Comment
        {
            PostId = postId,
            Id = $"{DateTime.UtcNow:o}#{userId}",
            Text = dto.Text
        };

        var save = await _commentsService.Save(comment);

        if (save.IsFailure)
            return StatusCode(500);
        
        var increment = await _postsService.IncrementTotalComments(postId);

        if (increment.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(string postId, [FromQuery] string? exclusiveStartKey = null)
    {
        var load = await _commentsService.LoadPage(postId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            comments = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

    [Authorize(CommentCreator)]
    [HttpDelete("{postId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(string postId, string commentId)
    {
        var delete = await _commentsService.Delete(postId, commentId);

        if (delete.IsFailure)
            return StatusCode(500);
        
        var decrement = await _postsService.DecrementTotalComments(postId);

        if (decrement.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpPost("{postId}/likes")]
    public async Task<IActionResult> CreateLike(string postId)
    {
        string userId = User.FindFirstValue("user_id")!;
        
        var like = new Like
        {
            PostId = postId,
            Id = $"{DateTime.UtcNow:o}#{userId}",
            UserId = userId
        };

        var save = await _likesService.Save(like);

        if (save.IsFailure)
            return StatusCode(500);
        
        var increment = await _postsService.IncrementTotalLikes(postId);

        if (increment.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpGet("{postId}/likes")]
    public async Task<IActionResult> GetLikes(string postId, [FromQuery] string? exclusiveStartKey = null)
    {
        var load = await _likesService.LoadPage(postId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            likes = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

    [Authorize(LikeCreator)]
    [HttpDelete("{postId}/likes/{likeId}")]
    public async Task<IActionResult> DeleteLike(string postId, string likeId)
    {
        var delete = await _likesService.Delete(postId, likeId);

        if (delete.IsFailure)
            return StatusCode(500);

        var decrement = await _postsService.DecrementTotalLikes(postId);

        if (decrement.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }
}
