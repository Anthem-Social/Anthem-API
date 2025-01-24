using AnthemAPI.Models;
using AnthemAPI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("posts")]
public class PostsController
(
    CommentsService commentsService,
    LikesService likesService,
    PostsService postsService
): ControllerBase
{
    private readonly CommentsService _commentsService = commentsService;
    private readonly LikesService _likesService = likesService;
    private readonly PostsService _postsService = postsService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostCreate dto)
    {
        var post = new Post
        {
            UserId = dto.UserId,
            Id = $"{DateTime.UtcNow:o}#{dto.UserId}",
            ContentType = dto.ContentType,
            Content = dto.Content,
            TotalLikes = 0,
            TotalComments = 0
        };

        var save = await _postsService.Save(post);

        if (save.IsFailure)
            return StatusCode(500);
        
        // TODO: add to friends' feeds

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

    [HttpDelete("{postId}")]
    public async Task<IActionResult> Delete(string postId)
    {
        var delete = await _postsService.Delete(postId);

        if (delete.IsFailure)
            return StatusCode(500);

        // TODO: remove from freinds' feeds
        
        return NoContent();
    }

    [HttpPost("{postId}/comments")]
    public async Task<IActionResult> CreateComment(string postId, [FromBody] CommentCreate dto)
    {
        var comment = new Comment
        {
            PostId = postId,
            Id = $"{DateTime.UtcNow:o}#{dto.UserId}",
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
        
        var data = new {
            comments = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

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

    [HttpPost("{postId}/likes/{userId}")]
    public async Task<IActionResult> CreateLike(string postId, string userId)
    {
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
        
        var data = new {
            likes = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

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
