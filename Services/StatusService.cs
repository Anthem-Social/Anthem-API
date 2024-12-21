using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class StatusService
{
    private readonly DynamoDBContext _context;

    public StatusService(IAmazonDynamoDB client)
    {
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Status?>> Load(string userId)
    {
        try
        {
            var status = await _context.LoadAsync<Status>(userId);
            return ServiceResult<Status?>.Success(status);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure(e, $"Failed to load for {userId}.", "StatusService.Load()");
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
            return ServiceResult<Status>.Failure(e, $"Failed to save for {status.UserId}.", "StatusService.Save()");
        }
    }
}
