using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;

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

    public async Task<ServiceResult<StatusConnection?>> Delete(string userId)
    {
        try
        {
            await _context.DeleteAsync<StatusConnection>(userId);
            return ServiceResult<StatusConnection?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<StatusConnection?>.Failure(e, $"Failed to delete for {userId}.", "StatusConnectionsService.Delete()");
        }
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
            var updates = new List<Task<UpdateItemResponse>>();

            userIds.ForEach(userId => {
                var request = new UpdateItemRequest
                {
                    TableName = TABLE_NAME,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue { S = userId } }
                    },
                    UpdateExpression = "ADD ConnectionIds :connectionId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":connectionId"] = new AttributeValue { SS = [connectionId] }
                    }
                };

                updates.Add(_client.UpdateItemAsync(request));
            });

            await Task.WhenAll(updates);

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
