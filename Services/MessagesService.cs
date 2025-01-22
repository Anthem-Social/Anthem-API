using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class MessagesService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const int PAGE_LIMIT = 20;
    private const string TABLE_NAME = "Messages";
    
    public MessagesService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
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
            return ServiceResult<Message>.Failure(e, $"Failed to save for {message.ChatId} and {message.Id}.", "MessagesService.Save()");
        }
    }

    public async Task<ServiceResult<Message?>> Delete(string chatId, string messageId)
    {
        try
        {
            var message = await _context.LoadAsync<Message>(chatId, messageId);

            if (message is not null)
                await _context.DeleteAsync(message);
            
            return ServiceResult<Message?>.Success(message);
        }
        catch (Exception e)
        {
            return ServiceResult<Message?>.Failure(e, $"Failed to delete for {chatId} and {messageId}.", "MessagesService.Delete()");
        }
    }

    public async Task<ServiceResult<(List<Message>, string?)>> LoadPage(string chatId, string? exclusiveStartKey = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                KeyConditionExpression = "ChatId = :chatId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":chatId"] = new AttributeValue { S = chatId }
                },
                ScanIndexForward = false,
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["ChatId"] = new AttributeValue { S = chatId },
                    ["Id"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Message> messages = response.Items
                .Select(message => new Message
                {
                    ChatId = message["ChatId"].S,
                    Id = message["Id"].S,
                    ContentType = (ContentType) int.Parse(message["ContentType"].N),
                    Content = message["Content"].S
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("Id")
                ? response.LastEvaluatedKey["Id"].S
                : null;

            return ServiceResult<(List<Message>, string?)>.Success((messages, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Message>, string?)>.Failure(e, $"Failed to load page for {chatId}.", "MessagesService.LoadPage()");
        }
    }
}
