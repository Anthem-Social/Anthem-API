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
}
