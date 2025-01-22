using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class CommentService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const int PAGE_LIMIT = 20;
    private const string TABLE_NAME = "Comments";

    public CommentService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
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
            return ServiceResult<bool>.Failure(e, $"Failed to delete {comment.Id} for {comment.PostId}.", "CommentService.Delete()");
        }
    }

    public async Task<ServiceResult<(List<Comment>, string?)>> LoadPage(string postId, string? exclusiveStartKey = null)
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

            List<Comment> comments = response.Items
                .Select(comment => new Comment
                {
                    PostId = comment["PostId"].S,
                    Id = comment["Id"].S,
                    Text = comment["Text"].S
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("Id")
                ? response.LastEvaluatedKey["Id"].S
                : null;

            return ServiceResult<(List<Comment>, string?)>.Success((comments, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Comment>, string?)>.Failure(e, $"Failed to load page for {postId}.", "CommentsService.LoadPage()");
        }
    }
}
