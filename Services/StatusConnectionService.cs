using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class StatusConnectionService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "StatusConnections";

    public StatusConnectionService(IAmazonDynamoDB client)
    {

        _client = client;
        _context = new DynamoDBContext(_client);
    }

    public async Task<ServiceResult<StatusConnection?>> Load(string userId)
    {
        try
        {
            var statusReceivers = await _context.LoadAsync<StatusConnection>(userId);
            return ServiceResult<StatusConnection?>.Success(statusReceivers);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection?>.Failure($"Failed to load status receivers for {userId}.\nError: {e}", "StatusReceiversService.Load()");
        }
    }

    public async Task<ServiceResult<StatusConnection>> Save(StatusConnection statusReceivers)
    {
        try
        {
            await _context.SaveAsync(statusReceivers);
            return ServiceResult<StatusConnection>.Success(statusReceivers);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection>.Failure($"Failed to save status receivers for {statusReceivers.UserId}.\nError: {e}", "StatusReceiversService.Save()");
        }
    }

    public async Task<ServiceResult<StatusConnection?>> AddConnectionId(List<string> userIds, string connectionId)
    {
        try
        {
            var batches = new List<Task<BatchExecuteStatementResponse>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_SIZE)
            {
                var ids = userIds.Skip(i).Take(DYNAMO_DB_BATCH_SIZE).ToList();

                var batch = new BatchExecuteStatementRequest
                {
                    Statements = ids.Select(userId => new BatchStatementRequest
                    {
                        Statement = $"UPDATE {TABLE_NAME}" +
                                    " SET Receivers = SET_ADD(Receivers, ?)" +
                                    " WHERE UserId = ?",
                        Parameters = new List<AttributeValue>
                        {
                            new AttributeValue { SS = [connectionId] },
                            new AttributeValue { S = userId }
                        }
                    }).ToList()
                };

                batches.Add(_client.BatchExecuteStatementAsync(batch));
            }

            await Task.WhenAll(batches);

            return ServiceResult<StatusConnection?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection?>.Failure($"Failed to batch add receiver {connectionId}.\nError: {e}", "StatusReceiversService.BatchAddReceiver()");
        }
    }

    public async Task<ServiceResult<int>> RemoveConnectionIds(string userId, List<string> connectionIds)
    {
        try
        {
            var request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } }
                },
                UpdateExpression = "DELETE Receivers :connectionIds",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":connectionIds"] = new AttributeValue
                    {
                        SS = connectionIds
                    }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            if (response.Attributes.ContainsKey("Receivers"))
            {
                int count = response.Attributes["Receivers"].SS.Count;
                return ServiceResult<int>.Success(count);
            }

            return ServiceResult<int>.Success(0);
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure($"Failed to remove receivers for {userId}.\nError: {e}", "StatusReceiversService.RemoveReceivers()");
        }
    }
}
