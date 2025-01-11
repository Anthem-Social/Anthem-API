using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class ChatService
{
    private readonly DynamoDBContext _context;
    
    public ChatService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<Chat?>> Load(string id)
    {
        try
        {
            var chat = await _context.LoadAsync<Chat>(id);
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
            await _context.DeleteAsync(id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to delete {id}.", "ChatService.Delete()");
        }
    }

    public async Task<ServiceResult<List<Chat>?>> GetBatch(List<string> ids)
    {
        try
        {
            BatchGet<Chat> batch = _context.CreateBatchGet<Chat>();
            ids.ForEach(batch.AddKey);
            await batch.ExecuteAsync();
            return ServiceResult<List<Chat>?>.Success(batch.Results);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Chat>?>.Failure(e, $"Failed to get batch.", "ChatService.GetBatch()");
        }
    }
}
