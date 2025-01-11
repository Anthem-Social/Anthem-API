using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class UserChatService
{
    private readonly DynamoDBContext _context;
    
    public UserChatService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<List<UserChat>>> GetBatch(string userId, int page)
    {
    try
    {
        var config = new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "UserId = :userId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":userId", userId }
                }
            },
            BackwardSearch = true,
            Limit = DYNAMO_DB_BATCH_SIZE,
            PaginationToken = page > 1 ? Helpers.CalculatePaginationToken(page) : null
        };

        var search = _context.FromQueryAsync<UserChat>(config);
        var userChats = new List<UserChat>();
        
        var results = await search.GetRemainingAsync();
        userChats.AddRange(results);

        return ServiceResult<List<UserChat>>.Success(userChats);
    }
    catch (Exception e)
    {
        return ServiceResult<List<UserChat>>.Failure(e, $"Failed to get batch.", "UserChatService.GetBatch()");
    }
    }
}
