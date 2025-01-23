using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
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
            string userId = postId.Split("#")[1];
            Post? post = await _context.LoadAsync<Post?>(userId, postId);
            return ServiceResult<Post?>.Success(post);
        }
        catch (Exception e)
        {
            return ServiceResult<Post?>.Failure(e, $"Failed to load {postId}.", "PostsService.Load()");
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
            return ServiceResult<Post>.Failure(e, $"Failed to save {post.Id}.", "PostsService.Save()");
        }
    }

    public async Task<ServiceResult<Post?>> Delete(string postId)
    {
        try
        {
            string userId = postId.Split("#")[1];
            await _context.DeleteAsync<Post>(userId, postId);
            return ServiceResult<Post?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Post?>.Failure(e, $"Failed to delete {postId}.", "PostsService.Delete()");
        }
    }

    public async Task<ServiceResult<int>> IncrementTotalLikes(string postId)
    {
        try
        {
            string userId = postId.Split("#")[1];
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
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostsService.IncrementTotalLikes()");
        }
    }

    public async Task<ServiceResult<int>> DecrementTotalLikes(string postId)
    {
        try
        {
            string userId = postId.Split("#")[1];
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
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostsService.DecrementTotalLikes()");
        }
    }

    public async Task<ServiceResult<int>> IncrementTotalComments(string postId)
    {
        try
        {
            string userId = postId.Split("#")[1];
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
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostsService.IncrementTotalComments()");
        }
    }

    public async Task<ServiceResult<int>> DecrementTotalComments(string postId)
    {
        try
        {
            string userId = postId.Split("#")[1];
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
            return ServiceResult<int>.Failure(e, $"Failed for {postId}.", "PostsService.DecrementTotalComments()");
        }
    }
}
