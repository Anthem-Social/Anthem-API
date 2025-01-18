using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class MessageService
{
    private readonly DynamoDBContext _context;
    
    public MessageService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<Message>> Save(Message message)
    {
        try
        {
            await _context.SaveAsync(message);
            return ServiceResult<Message>.Success(message);
        }
        catch (Exception e)
        {
            return ServiceResult<Message>.Failure(e, $"Failed to save for {message.ChatId} and {message.Id}.", "MessageService.Save()");
        }
    }

    public async Task<ServiceResult<Message?>> Delete(string chatId, string id)
    {
        try
        {
            var message = await _context.LoadAsync<Message>(chatId, id);

            if (message is not null)
                await _context.DeleteAsync(message);
            
            return ServiceResult<Message?>.Success(message);
        }
        catch (Exception e)
        {
            return ServiceResult<Message?>.Failure(e, $"Failed to delete for {chatId} and {id}.", "MessageService.Delete()");
        }
    }

    public async Task<ServiceResult<List<Message>>> LoadBatch(string chatId, int page)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "ChatId = :chatId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":chatId", chatId }
                    }
                },
                BackwardSearch = true,
                Limit = MESSAGE_BATCH_LIMIT
            };

            var search = _context.FromQueryAsync<Message>(query);

            var messages = new List<Message>();

            for (int x = 0; x < page; x++)
            {
                messages = await search.GetNextSetAsync();
            }

            return ServiceResult<List<Message>>.Success(messages);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Message>>.Failure(e, "Failed to load batch.", "MessageService.LoadBatch()");
        }
    }
}
