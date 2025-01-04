using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class LikeService
{
    private readonly DynamoDBContext _context;
    
    public LikeService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<Like>> Save(Like like)
    {
        try
        {
            await _context.SaveAsync(like);
            return ServiceResult<Like>.Success(like);
        }
        catch (Exception e)
        {
            return ServiceResult<Like>.Failure(e, $"Failed to save {like.UserId} for {like.PostId}.", "LikeService.Save()");
        }
    }

    public async Task<ServiceResult<List<Like>?>> GetAll(string postId)
    {
        try
        {
            var search = _context.QueryAsync<Like>(postId);
            var likes = await search.GetRemainingAsync();
            return ServiceResult<List<Like>?>.Success(likes);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Like>?>.Failure(e, $"Failed to get all for {postId}.", "LikeService.GetAll()");
        }
    }

    public async Task<ServiceResult<bool>> Delete(Like like)
    {
        try
        {
            await _context.DeleteAsync(like);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to delete {like.UserId} for {like.PostId}.", "LikeService.Save()");
        }
    }
}
