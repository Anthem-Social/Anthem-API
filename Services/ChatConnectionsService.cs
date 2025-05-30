using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;

namespace AnthemAPI.Services;

public class ChatConnectionsService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "ChatConnections";

    public ChatConnectionsService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<ChatConnection?>> Load(string chatId)
    {
        try
        {
            ChatConnection? chatConnection = await _context.LoadAsync<ChatConnection>(chatId);
            return ServiceResult<ChatConnection?>.Success(chatConnection);
        }
        catch (Exception e)
        {
            return ServiceResult<ChatConnection?>.Failure(e, $"Failed to load for {chatId}.", "ChatConnectionsService.Load()");
        }
    }

    public async Task<ServiceResult<ChatConnection>> AddConnection(string chatId, string connectionId)
    {
        try
        {
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "ChatId", new AttributeValue { S = chatId } }
                },
                UpdateExpression = "ADD ConnectionIds :connectionId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":connectionId"] = new AttributeValue { SS = [connectionId] }
                },
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            var chatConnection = new ChatConnection
            {
                ChatId = response.Attributes["ChatId"].S,
                ConnectionIds = response.Attributes["ConnectionIds"].SS.ToHashSet()
            };

            return ServiceResult<ChatConnection>.Success(chatConnection);
        }
        catch (Exception e)
        {
            return ServiceResult<ChatConnection>.Failure(e, $"Failed to add connection to {chatId}.", "ChatConnectionsService.AddConnectionId()");
        }
    }

    public async Task<ServiceResult<ChatConnection>> RemoveConnections(string chatId, List<string> connectionIds)
    {
        try
        {
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "ChatId", new AttributeValue { S = chatId } }
                },
                UpdateExpression = "DELETE ConnectionIds :connectionIds",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":connectionIds"] = new AttributeValue { SS = connectionIds }
                },
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            var chatConnection = new ChatConnection
            {
                ChatId = response.Attributes["ChatId"].S,
                ConnectionIds = response.Attributes["ConnectionIds"].SS.ToHashSet()
            };

            return ServiceResult<ChatConnection>.Success(chatConnection);
        }
        catch (Exception e)
        {
            return ServiceResult<ChatConnection>.Failure(e, $"Failed to remove connection from {chatId}.", "ChatConnectionsService.RemoveConnectionId()");
        }
    }
}
