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
    PostsService postsService,
    UsersService usersService
) : ControllerBase
{
    private readonly CommentsService _commentsService = commentsService;
    private readonly FeedsService _feedsService = feedsService;
    private readonly FollowersService _followersService = followersService;
    private readonly LikesService _likesService = likesService;
    private readonly PostsService _postsService = postsService;
    private readonly UsersService _usersService = usersService;

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
            Caption = dto.Caption,
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
        
        if (loadFriends.Data is null || loadFriends.Data.Count == 0)
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
        // Load the Post
        var loadPost = await _postsService.Load(postId);

        if (loadPost.IsFailure)
            return StatusCode(500);
        
        if (loadPost.Data is null)
            return NotFound();
        
        Post post = loadPost.Data;

        // Load the User
        var loadUser = await _usersService.Load(post.UserId);

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

        // Create the PostCard DTO
        var postCard = new PostCard
        {
            Post = post,
            Card = card
        };

        return Ok(postCard);
    }

    [Authorize(PostCreator)]
    [HttpDelete("{postId}")]
    public async Task<IActionResult> Delete(string postId)
    {
        string userId = User.FindFirstValue("user_id")!;

        // Delete the Post
        var deletePost = await _postsService.Delete(postId);

        if (deletePost.IsFailure)
            return StatusCode(500);

        // Delete all the Likes for the Post
        var deleteLikes = await _likesService.DeleteAllByPostId(postId);

        if (deleteLikes.IsFailure)
            return StatusCode(500);

        // Load the User's friends
        var loadFriends = await _followersService.LoadFriends(userId);

        if (loadFriends.IsFailure)  
            return StatusCode(500);
        
        if (loadFriends.Data is not null && loadFriends.Data.Count > 0)
            return NoContent();
        
        List<string> userIds = loadFriends.Data!.Select(f => f.FollowerUserId).ToList();

        // Delete the Post from all their Feeds
        var deleteAll = await _feedsService.DeletePostFromAll(userIds, postId);

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
        // Load the page of Comments
        var loadComments = await _commentsService.LoadPage(postId, exclusiveStartKey);

        if (loadComments.IsFailure)
            return StatusCode(500);

        List<Comment> comments = loadComments.Data.Item1;
        string? lastEvaluatedKey = loadComments.Data.Item2;
        HashSet<string> userIds = comments.Select(comment => comment.Id.Split("#")[1]).ToHashSet();

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create lookup dictionary
        Dictionary<string, Card> dict = cards.ToDictionary(card => card.UserId);

        // Create list of CommentCard DTOs
        List<CommentCard> commentCards = comments
            .Select(comment => new CommentCard
            {
                Card = dict[comment.Id.Split("#")[1]],
                Comment = comment
            })
            .ToList();

        // Create data to return
        var data = new
        {
            comments = commentCards,
            lastEvaluatedKey
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
        
        return CreatedAtAction(
            nameof(DeleteLike),
            new { likeId = like.Id, postId },
            like
        );
    }

    [HttpGet("{postId}/likes")]
    public async Task<IActionResult> GetLikes(string postId, [FromQuery] string? exclusiveStartKey = null)
    {
        // Load the page of Likes
        var loadLikes = await _likesService.LoadPage(postId, exclusiveStartKey);

        if (loadLikes.IsFailure)
            return StatusCode(500);
        
        List<Like> likes = loadLikes.Data.Item1;
        string? lastEvaluatedKey = loadLikes.Data.Item2;
        HashSet<string> userIds = likes.Select(like => like.UserId).ToHashSet();

        // Get the Cards
        var getCards = await _usersService.GetCards(userIds);

        if (getCards.IsFailure)
            return StatusCode(500);
        
        List<Card> cards = getCards.Data!;

        // Create lookup dictionary
        Dictionary<string, Card> dict = cards.ToDictionary(card => card.UserId);

        // Create list of LikeCards DTOs
        List<LikeCard> likeCards = likes
            .Select(like => new LikeCard
            {
                Card = dict[like.UserId],
                Like = like
            })
            .ToList();

        // Create data to return
        var data = new
        {
            lastEvaluatedKey,
            results = likeCards
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
