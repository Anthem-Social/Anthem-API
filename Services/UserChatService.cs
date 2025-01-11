using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;
using Microsoft.AspNetCore.Components.Web;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class UserChatService
{
    private readonly DynamoDBContext _context;
    
    public UserChatService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }
    
    public async Task<ServiceResult<UserChat>> Save(UserChat userChat)
    {
        try
        {
            await _context.SaveAsync(userChat);
            return ServiceResult<UserChat>.Success(userChat);
        }
        catch (Exception e)
        {
            return ServiceResult<UserChat>.Failure(e, $"Failed to save.", "UserChatService.Save()");
        }
    }

    public async Task<ServiceResult<bool>> Delete(string userId, string chatId)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "UserId = :userId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":userId", userId }
                    }
                },
                Filter = new QueryFilter("ChatId", QueryOperator.Equal, chatId)
            };

            var search = _context.FromQueryAsync<UserChat>(query);

            List<UserChat> userChats = await search.GetRemainingAsync();

            var deletions = userChats.Select(async x => await _context.DeleteAsync(x));

            await Task.WhenAll(deletions);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to delete {chatId} for {userId}.", "UserChatService.Delete()");
        }
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
