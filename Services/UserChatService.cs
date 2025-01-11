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

    public async Task<ServiceResult<UserChat?>> Load(string userId, string chatId)
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

            if (userChats.Count > 1)
            {
                return ServiceResult<UserChat?>.Failure(null, $"More than one found for {userId} and {chatId}.", "UserChatService.Load()");
            }

            return ServiceResult<UserChat?>.Success(userChats.FirstOrDefault(defaultValue: null));
        }
        catch (Exception e)
        {
            return ServiceResult<UserChat?>.Failure(e, "Failed to load.", "UserChatService.Load()");
        }
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
            return ServiceResult<UserChat>.Failure(e, "Failed to save.", "UserChatService.Save()");
        }
    }

    public async Task<ServiceResult<UserChat>> Delete(UserChat userChat)
    {
        try
        {
            await _context.DeleteAsync(userChat);
            return ServiceResult<UserChat>.Success(userChat);
        }
        catch (Exception e)
        {
            return ServiceResult<UserChat>.Failure(e, $"Failed to delete {userChat.ChatId} for {userChat.UserId}.", "UserChatService.Delete()");
        }
    }

    public async Task<ServiceResult<List<UserChat>>> LoadBatch(string userId, int page)
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
                BackwardSearch = true,
                Limit = USER_CHAT_BATCH_LIMIT,
                PaginationToken = page > 1 ? Helpers.CalculatePaginationToken(page, USER_CHAT_BATCH_LIMIT) : null
            };

            var search = _context.FromQueryAsync<UserChat>(query);

            List<UserChat> userChats = await search.GetRemainingAsync();

            return ServiceResult<List<UserChat>>.Success(userChats);
        }
        catch (Exception e)
        {
            return ServiceResult<List<UserChat>>.Failure(e, "Failed to load batch.", "UserChatService.LoadBatch()");
        }
    }
}
