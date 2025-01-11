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
            return ServiceResult<Message>.Failure(e, $"Failed to save to {message.ChatId} for {message.UserId}.", "MessageService.Save()");
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
                Limit = MESSAGE_BATCH_LIMIT,
                PaginationToken = page > 1 ? Helpers.CalculatePaginationToken(page, MESSAGE_BATCH_LIMIT) : null
            };

            var search = _context.FromQueryAsync<Message>(query);

            List<Message> messages = await search.GetRemainingAsync();

            return ServiceResult<List<Message>>.Success(messages);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Message>>.Failure(e, "Failed to load batch.", "MessageService.LoadBatch()");
        }
    }
}
