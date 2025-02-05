using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class StatusConnectionsService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const string TABLE_NAME = "StatusConnections";

    public StatusConnectionsService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(_client);
    }

    public async Task<ServiceResult<StatusConnection?>> Load(string userId)
    {
        try
        {
            StatusConnection? statusConnection = await _context.LoadAsync<StatusConnection>(userId);
            return ServiceResult<StatusConnection?>.Success(statusConnection);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection?>.Failure(e, $"Failed to load for {userId}.", "StatusConnectionsService.Load()");
        }
    }

    public async Task<ServiceResult<StatusConnection>> Save(StatusConnection statusConnection)
    {
        try
        {
            await _context.SaveAsync(statusConnection);
            return ServiceResult<StatusConnection>.Success(statusConnection);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection>.Failure(e, $"Failed to save for {statusConnection.UserId}.", "StatusConnectionsService.Save()");
        }
    }

    public async Task<ServiceResult<StatusConnection>> Clear(string userId)
    {
        try
        {
            var empty = new StatusConnection
            {
                UserId = userId,
                ConnectionIds = new HashSet<string>()
            };

            return await Save(empty);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection>.Failure(e, $"Failed to clear for {userId}.", "StatusConnectionsService.Clear()");
        }
    }

    public async Task<ServiceResult<StatusConnection?>> AddConnectionToAll(List<string> userIds, string connectionId)
    {
        try
        {
            var batches = new List<Task<BatchExecuteStatementResponse>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT)
            {
                List<string> ids = userIds.Skip(i).Take(DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT).ToList();
                var batch = new BatchExecuteStatementRequest
                {
                    Statements = ids.Select(userId => new BatchStatementRequest
                    {
                        Statement = $"UPDATE {TABLE_NAME}" +
                                    " SET ConnectionIds = SET_ADD(ConnectionIds, ?)" +
                                    " WHERE UserId = ?",
                        Parameters = new List<AttributeValue>
                        {
                            new AttributeValue { SS = [ connectionId ] },
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
            return ServiceResult<StatusConnection?>.Failure(e, $"Failed to add connection {connectionId}.", "StatusConnectionsService.AddConnection()");
        }
    }

    public async Task<ServiceResult<int>> RemoveConnections(string userId, List<string> connectionIds)
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
                UpdateExpression = "DELETE ConnectionIds :connectionIds",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":connectionIds"] = new AttributeValue { SS = connectionIds }
                },
                ReturnValues = ReturnValue.UPDATED_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            if (response.Attributes.ContainsKey("ConnectionIds"))
            {
                int count = response.Attributes["ConnectionIds"].SS.Count;
                return ServiceResult<int>.Success(count);
            }

            return ServiceResult<int>.Success(0);
        }
        catch (Exception e)
        {
            return ServiceResult<int>.Failure(e, $"Failed to remove connections for {userId}.", "StatusConnectionsService.RemoveConnections()");
        }
    }
}
