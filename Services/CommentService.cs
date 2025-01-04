using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class CommentService
{
    private readonly DynamoDBContext _context;
    
    public CommentService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<Comment>> Save(Comment comment)
    {
        try
        {
            await _context.SaveAsync(comment);
            return ServiceResult<Comment>.Success(comment);
        }
        catch (Exception e)
        {
            return ServiceResult<Comment>.Failure(e, $"Failed to save {comment.Id} for {comment.PostId}.", "CommentService.Save()");
        }
    }

    public async Task<ServiceResult<List<Comment>?>> GetAll(string postId)
    {
        try
        {
            var search = _context.QueryAsync<Comment>(postId);
            var comments = await search.GetRemainingAsync();
            return ServiceResult<List<Comment>?>.Success(comments);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Comment>?>.Failure(e, $"Failed to get all for {postId}.", "CommentService.GetAll()");
        }
    }

    public async Task<ServiceResult<bool>> Delete(Comment comment)
    {
        try
        {
            await _context.DeleteAsync(comment);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to delete {comment.UserId} for {comment.PostId}.", "CommentService.Save()");
        }
    }
}
