using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class UserService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Users";

    public UserService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<User?>> Load(string userId)
    {
        try
        {
            var user = await _context.LoadAsync<User>(userId);
            return ServiceResult<User?>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to load for {userId}.", "UserService.Load()");
        }
    }

    public async Task<ServiceResult<User>> Save(User user)
    {
        try
        {
            await _context.SaveAsync(user);
            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure(e, $"Failed to save for {user.Id}.", "UserService.Save()");
        }
    }

    public async Task<ServiceResult<User?>> AddChatIdToMembers(List<string> userIds, string chatId)
    {
        try
        {
            var batch = new BatchExecuteStatementRequest
            {
                Statements = userIds.Select(userId => new BatchStatementRequest
                {
                    Statement = $"UPDATE {TABLE_NAME}" +
                                " SET ChatIds = SET_ADD(ChatIds, ?)" +
                                " WHERE Id = ?",
                    Parameters = new List<AttributeValue>
                    {
                        new AttributeValue { SS = [chatId] },
                        new AttributeValue { S = userId }
                    }
                }).ToList()
            };

            await _client.BatchExecuteStatementAsync(batch);

            return ServiceResult<User?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to add chat id.", "UserService.AddChatId()");
        }
    }
}
