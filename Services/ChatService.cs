using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class ChatService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "Chats";
    
    public ChatService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Chat?>> Load(string id)
    {
        try
        {
            var chat = await _context.LoadAsync<Chat?>(id);
            return ServiceResult<Chat?>.Success(chat);
        }
        catch (Exception e)
        {
            return ServiceResult<Chat?>.Failure(e, $"Failed to load for {id}.", "ChatService.Load()");
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
            return ServiceResult<Chat>.Failure(e, $"Failed to save for {chat.Id}.", "ChatService.Save()");
        }
    }

    public async Task<ServiceResult<bool>> Delete(string id)
    {
        try
        {
            var load = await Load(id);
            await _context.DeleteAsync(load.Data);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to delete {id}.", "ChatService.Delete()");
        }
    }

    public async Task<ServiceResult<bool>> Update(string id, DateTime lastMessageAt, string preview)
    {
        try
        {
            var update = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["Id"] = new AttributeValue { S = id }
                },
                UpdateExpression = "SET Preview = :preview, LastMessageAt = :lastMessageAt",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":preview"] = new AttributeValue { S = preview },
                    [":lastMessageAt"] = new AttributeValue { S = lastMessageAt.ToString("o") }
                }
            };

            var response = await _client.UpdateItemAsync(update);

            Console.WriteLine("attributes: " + response.Attributes);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, "Failed to update.", "ChatService.Update()");
        }
    }

    public async Task<ServiceResult<List<Chat>>> GetPage(List<string> ids, int page)
    {
        try
        {
            var batches = new List<BatchGet<Chat>>();

            for (int i = 0; i < ids.Count; i += DYNAMO_DB_BATCH_GET_ITEM_LIMIT)
            {
                List<string> chatIds  = ids.Skip(i).Take(DYNAMO_DB_BATCH_GET_ITEM_LIMIT).ToList();
                var batch = _context.CreateBatchGet<Chat>();
                chatIds.ForEach(batch.AddKey);
                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            var chats = batches
                .SelectMany(b => b.Results)
                .OrderByDescending(c => c.LastMessageAt)
                .Skip(page > 1 ? Helpers.CalculatePaginationToken(page, CHAT_BATCH_LIMIT) : 0)
                .Take(CHAT_BATCH_LIMIT)
                .ToList();

            return ServiceResult<List<Chat>>.Success(chats);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Chat>>.Failure(e, "Failed to get all.", "ChatService.GetAll()");
        }
    }
}
