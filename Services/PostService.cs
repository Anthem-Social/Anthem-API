using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class PostsService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Posts";

    public PostsService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Post?>> Load(string postId)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                IndexName = "Id-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Id = :id",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        {":id", postId}
                    }
                }
            };

            var search = _context.FromQueryAsync<Post?>(query);

            var results = await search.GetRemainingAsync();

            if (results.Count == 0)
                return ServiceResult<Post?>.Success(null);

            if (results.Count > 1)
                return ServiceResult<Post?>.Failure(null, "More than one result.", "PostServcie.Load()");

            return ServiceResult<Post?>.Success(results[0]);
        }
        catch (Exception e)
        {
            return ServiceResult<Post?>.Failure(e, $"Failed to load {postId}.", "PostService.Load()");
        }
    }

    public async Task<ServiceResult<Post>> Save(Post post)
    {
        try
        {
            await _context.SaveAsync(post);
            return ServiceResult<Post>.Success(post);
        }
        catch (Exception e)
        {
            return ServiceResult<Post>.Failure(e, $"Failed to save {post.Id}.", "PostService.Save()");
        }
    }

    public async Task<ServiceResult<Post?>> Delete(string postId)
    {
        try
        {
            var load = await Load(postId);

            if (load.IsFailure)
                return load;

            if (load.Data is null)
                return ServiceResult<Post?>.Success(null);
            
            await _context.DeleteAsync(load.Data);

            return ServiceResult<Post?>.Success(load.Data);
        }
        catch (Exception e)
        {
            return ServiceResult<Post?>.Failure(e, $"Failed to delete {postId}.", "PostService.Delete()");
        }
    }

    public async Task<ServiceResult<int>> IncrementTotalLikes(string postId)
    {
        try
        {
            var userId = postId.Split("#")[1];
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "Id", new AttributeValue { S = postId } }
                },
                UpdateExpression = "ADD TotalLikes :one",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":one"] = new AttributeValue { N = "1" }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };
            var response = await _client.UpdateItemAsync(request);
            var totalLikes = response.Attributes["TotalLikes"].N;

            return ServiceResult<int>.Success(int.Parse(totalLikes));
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostService.IncrementTotalLikes()");
        }
    }

    public async Task<ServiceResult<int>> DecrementTotalLikes(string postId)
    {
        try
        {
            var userId = postId.Split("#")[1];
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "Id", new AttributeValue { S = postId } }
                },
                UpdateExpression = "ADD TotalLikes :negativeOne",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":negativeOne"] = new AttributeValue { N = "-1" }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };
            var response = await _client.UpdateItemAsync(request);
            var totalLikes = response.Attributes["TotalLikes"].N;

            return ServiceResult<int>.Success(int.Parse(totalLikes));
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostService.DecrementTotalLikes()");
        }
    }

    public async Task<ServiceResult<int>> IncrementTotalComments(string postId)
    {
        try
        {
            var userId = postId.Split("#")[1];
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "Id", new AttributeValue { S = postId } }
                },
                UpdateExpression = "ADD TotalComments :one",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":one"] = new AttributeValue { N = "1" }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };
            var response = await _client.UpdateItemAsync(request);
            var totalComments = response.Attributes["TotalComments"].N;

            return ServiceResult<int>.Success(int.Parse(totalComments));
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostService.IncrementTotalComments()");
        }
    }

    public async Task<ServiceResult<int>> DecrementTotalComments(string postId)
    {
        try
        {
            var userId = postId.Split("#")[1];
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "Id", new AttributeValue { S = postId } }
                },
                UpdateExpression = "ADD TotalComments :negativeOne",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":negativeOne"] = new AttributeValue { N = "-1" }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };
            var response = await _client.UpdateItemAsync(request);
            var totalComments = response.Attributes["TotalComments"].N;

            return ServiceResult<int>.Success(int.Parse(totalComments));
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostService.DecrementTotalComments()");
        }
    }
}
