using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class MessageService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Messages";
    
    public MessageService(IAmazonDynamoDB client)
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
            return ServiceResult<Message>.Failure(e, $"Failed to save for {message.ChatId} and {message.Id}.", "MessageService.Save()");
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
            return ServiceResult<Message?>.Failure(e, $"Failed to delete for {chatId} and {messageId}.", "MessageService.Delete()");
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
                Limit = MESSAGE_BATCH_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                var keys = exclusiveStartKey.Split("::");
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["ChatId"] = new AttributeValue { S = keys[0] },
                    ["Id"] = new AttributeValue { S = keys[1] }
                };
            }
            
            var response = await _client.QueryAsync(request);

            Console.WriteLine(JsonSerializer.Serialize(response.Items[0]));

            List<Message> messages = response.Items
                .Select(message => new Message
                {
                    ChatId = message["ChatId"].S,
                    Id = message["Id"].S,
                    ContentType = (ContentType) int.Parse(message["ContentType"].N),
                    Content = message["Content"].S
                })
                .ToList();

            Console.WriteLine("Length: " + messages.Count);
            // will fail on last page because these properties won't exist
            // use Id only in the exclusiveStartKey and lastEvaluatedKey
            string lastEvaluatedKey = response.LastEvaluatedKey["ChatId"].S + "::" + response.LastEvaluatedKey["Id"].S;

            return ServiceResult<(List<Message>, string?)>.Success((messages, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Message>, string?)>.Failure(e, $"Failed to load page for {chatId}", "MessageService.LoadPage()");
        }
    }
}
