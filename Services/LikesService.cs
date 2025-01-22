using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class LikesService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const int PAGE_LIMIT = 20;
    private const string TABLE_NAME = "Likes"; 
    
    public LikesService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Like?>> Load(string userId, string postId)
    {
        var query = new QueryOperationConfig
        {
            IndexName = "UserId-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "UserId = :userId AND PostId = :postId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":userId", userId },
                    { ":postId", postId }
                }
            }
        };

        var search = _context.FromQueryAsync<Like?>(query);
        var results = await search.GetRemainingAsync();

        if (results.Count == 0)
            return ServiceResult<Like?>.Success(null);

        if (results.Count > 1)
            return ServiceResult<Like?>.Failure(null, "More than one result.", "LikesService.Load()");
        
        return ServiceResult<Like?>.Success(results[0]);
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
            return ServiceResult<Like>.Failure(e, $"Failed to save {like.Id} for {like.PostId}.", "LikesService.Save()");
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
            return ServiceResult<List<Like>?>.Failure(e, $"Failed to get all for {postId}.", "LikesService.GetAll()");
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
            return ServiceResult<bool>.Failure(e, $"Failed to delete {like.Id} for {like.PostId}.", "LikesService.Delete()");
        }
    }

    public async Task<ServiceResult<(List<Like>, string?)>> LoadPage(string postId, string? exclusiveStartKey = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                KeyConditionExpression = "PostId = :postId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":postId"] = new AttributeValue { S = postId }
                },
                ScanIndexForward = false,
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["PostId"] = new AttributeValue { S = postId },
                    ["Id"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Like> likes = response.Items
                .Select(like => new Like
                {
                    PostId = like["PostId"].S,
                    Id = like["Id"].S,
                    UserId = like["UserId"].S
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("Id")
                ? response.LastEvaluatedKey["Id"].S
                : null;

            return ServiceResult<(List<Like>, string?)>.Success((likes, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Like>, string?)>.Failure(e, $"Failed to load page for {postId}.", "LikesService.LoadPage()");
        }
    }
}
