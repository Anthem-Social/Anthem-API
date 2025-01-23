using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class ChatsService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const int PAGE_LIMIT = 20;
    private const string TABLE_NAME = "Chats";
    
    public ChatsService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Chat?>> Load(string chatId)
    {
        try
        {
            var chat = await _context.LoadAsync<Chat?>(chatId);
            return ServiceResult<Chat?>.Success(chat);
        }
        catch (Exception e)
        {
            return ServiceResult<Chat?>.Failure(e, $"Failed to load for {chatId}.", "ChatsService.Load()");
        }
    }

    public async Task<ServiceResult<Chat>> Save(Chat chat)
    {
        try
        {
            await _context.SaveAsync(chat);
            return ServiceResult<Chat>.Success(chat);
        }
        catch (Exception e)
        {
            return ServiceResult<Chat>.Failure(e, $"Failed to save for {chat.Id}.", "ChatsService.Save()");
        }
    }

    public async Task<ServiceResult<Chat?>> Delete(string chatId)
    {
        try
        {
            var load = await Load(chatId);

            if (load.IsFailure || load.Data is null)
                return load;
            
            await _context.DeleteAsync(load.Data);

            return ServiceResult<Chat?>.Success(load.Data);
        }
        catch (Exception e)
        {
            return ServiceResult<Chat?>.Failure(e, $"Failed to delete {chatId}.", "ChatsService.Delete()");
        }
    }

    public async Task<ServiceResult<Chat>> Update(string chatId, DateTime lastMessageAt, string preview)
    {
        try
        {
            var update = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { S = chatId }
                },
                UpdateExpression = "SET Preview = :preview, LastMessageAt = :lastMessageAt",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":preview"] = new AttributeValue { S = preview },
                    [":lastMessageAt"] = new AttributeValue { S = lastMessageAt.ToString("o") }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };

            var response = await _client.UpdateItemAsync(update);

            var chat = new Chat
            {
                Id = response.Attributes["Id"].S,
                Name = response.Attributes["Name"].S,
                UserIds = response.Attributes["UserIds"].SS.ToHashSet(),
                LastMessageAt = Helpers.ToDateTimeUTC(response.Attributes["LastMessageAt"].S),
                Preview = response.Attributes["Preview"].S,
                CreatorUserId = response.Attributes["CreatorUserId"].S,
                CreatedAt = Helpers.ToDateTimeUTC(response.Attributes["CreatedAt"].S)
            };

            return ServiceResult<Chat>.Success(chat);
        }
        catch (Exception e)
        {
            return ServiceResult<Chat>.Failure(e, $"Failed to update for {chatId}.", "ChatsService.Update()");
        }
    }

    public async Task<ServiceResult<List<Chat>>> GetPage(List<string> chatIds, int page)
    {
        try
        {
            var batches = new List<BatchGet<Chat>>();

            for (int i = 0; i < chatIds.Count; i += DYNAMO_DB_BATCH_GET_LIMIT)
            {
                List<string> ids  = chatIds.Skip(i).Take(DYNAMO_DB_BATCH_GET_LIMIT).ToList();
                var batch = _context.CreateBatchGet<Chat>();
                ids.ForEach(batch.AddKey);
                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            var chats = batches
                .SelectMany(b => b.Results)
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(page > 1 ? (page - 1) * PAGE_LIMIT : 0)
                .Take(PAGE_LIMIT)
                .ToList();

            return ServiceResult<List<Chat>>.Success(chats);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Chat>>.Failure(e, "Failed to get page.", "ChatsService.GetPage()");
        }
    }
}
