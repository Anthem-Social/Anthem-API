using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class StatusesService
{
    private readonly DynamoDBContext _context;

    public StatusesService(IAmazonDynamoDB client)
    {
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Status?>> Delete(string userId)
    {
        try
        {
            await _context.DeleteAsync<Status>(userId);
            return ServiceResult<Status?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Status?>.Failure(e, $"Failed to delete status for {userId}.", "StatusesService.Delete()");
        }
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

    public async Task<ServiceResult<List<Status>>> LoadAll(List<string> userIds)
    {
        try
        {
            var batches = new List<BatchGet<Status>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_GET_LIMIT)
            {
                List<string> ids  = userIds.Skip(i).Take(DYNAMO_DB_BATCH_GET_LIMIT).ToList();
                var batch = _context.CreateBatchGet<Status>();
                ids.ForEach(batch.AddKey);
                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            List<Status> statuses = batches
                .SelectMany(batch => batch.Results)
                .ToList();
            
            return ServiceResult<List<Status>>.Success(statuses);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Status>>.Failure(e, "Failed to load all.", "StatusesService.LoadAll()");
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
