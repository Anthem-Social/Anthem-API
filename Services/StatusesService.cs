using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class StatusesService
{
    private readonly DynamoDBContext _context;

    public StatusesService(IAmazonDynamoDB client)
    {
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Status?>> Load(string userId)
    {
        try
        {
            Status? status = await _context.LoadAsync<Status>(userId);
            return ServiceResult<Status?>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure(e, $"Failed to load for {userId}.", "StatusesService.Load()");
        }
    }

    public async Task<ServiceResult<Status>> Save(Status status)
    {
        try
        {
            await _context.SaveAsync(status);
            return ServiceResult<Status>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status>.Failure(e, $"Failed to save for {status.UserId}.", "StatusesService.Save()");
        }
    }
}
