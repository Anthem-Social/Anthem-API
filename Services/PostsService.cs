using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class PostsService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Posts";
    private const int PAGE_LIMIT = 21;

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

    public async Task<ServiceResult<List<Post>>> Load(List<string> postIds)
    {
        try
        {
            var batches = new List<BatchGet<Post>>();

            for (int i = 0; i < postIds.Count; i += DYNAMO_DB_BATCH_GET_LIMIT)
            {
                List<string> ids  = postIds.Skip(i).Take(DYNAMO_DB_BATCH_GET_LIMIT).ToList();
                var batch = _context.CreateBatchGet<Post>();

                foreach (string id in ids)
                {
                    string userId = id.Split("#")[1];
                    batch.AddKey(userId, id);
                }

                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            List<Post> posts = batches
                .SelectMany(batch => batch.Results)
                .ToList();
            
            return ServiceResult<List<Post>>.Success(posts);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Post>>.Failure(e, "Failed to load all.", "PostsService.LoadAll()");
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

    public async Task<ServiceResult<(List<Post>, string?)>> LoadPage(string userId, string? exclusiveStartKey = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                KeyConditionExpression = "UserId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                },
                ScanIndexForward = false,
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["UserId"] = new AttributeValue { S = userId },
                    ["Id"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Post> posts = response.Items
                .Select(post => new Post
                {
                    UserId = post["UserId"].S,
                    Id = post["Id"].S,
                    ContentType = (ContentType) int.Parse(post["ContentType"].N),
                    Content = post["Content"].S,
                    Text = post.ContainsKey("Text")
                        ? post["Text"].S
                        : null,
                    TotalLikes = long.Parse(post["TotalLikes"].N),
                    TotalComments = long.Parse(post["TotalComments"].N)
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("Id")
                ? response.LastEvaluatedKey["Id"].S
                : null;

            return ServiceResult<(List<Post>, string?)>.Success((posts, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Post>, string?)>.Failure(e, $"Failed to load page for {userId}.", "PostsService.LoadPage()");
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
