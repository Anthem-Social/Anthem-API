using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class FeedsService
{
    private readonly IAmazonDynamoDB _client;
    private const int PAGE_LIMIT = 30;
    private const string TABLE_NAME = "Feeds";

    public FeedsService(IAmazonDynamoDB client)
    {
        _client = client;
    }

    public async Task<ServiceResult<(List<Feed>, string?)>> LoadPage(string userId, string? exclusiveStartKey = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                KeyConditionExpression = "UserId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                },
                ScanIndexForward = false,
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["UserId"] = new AttributeValue { S = userId },
                    ["PostId"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Feed> feed = response.Items
                .Select(message => new Feed
                {
                    UserId = message["UserId"].S,
                    PostId = message["PostId"].S,
                    ExpiresAt = long.Parse(message["ExpiresAt"].N)
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("PostId")
                ? response.LastEvaluatedKey["PostId"].S
                : null;

            return ServiceResult<(List<Feed>, string?)>.Success((feed, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Feed>, string?)>.Failure(e, $"Failed to load page for {userId}.", "FeedsService.LoadPage()");
        }
    }

    public async Task<ServiceResult<Feed?>> SaveAll(List<string> userIds, string postId)
    {
        try
        {
            var batches = new List<Task<BatchWriteItemResponse>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_WRITE_LIMIT)
            {
                List<string> ids = userIds.Skip(i).Take(DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT).ToList();
                var batch = new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [TABLE_NAME] = ids.Select(id => new WriteRequest
                        {
                            PutRequest = new PutRequest
                            {
                                Item = new Dictionary<string, AttributeValue>
                                {
                                    ["UserId"] = new AttributeValue { S = id },
                                    ["PostId"] = new AttributeValue { S = postId },
                                    ["ExpiresAt"] = new AttributeValue { N = DateTimeOffset.UtcNow.AddDays(FEEDS_TTL_DAYS).ToUnixTimeSeconds().ToString() }
                                }
                            }
                        }).ToList()
                    }
                };
                batches.Add(_client.BatchWriteItemAsync(batch));
            }

            await Task.WhenAll(batches);
            
            return ServiceResult<Feed?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Feed?>.Failure(e, $"Failed to save all for {postId}.", "FeedsService.SaveAll()");
        }
    }

    public async Task<ServiceResult<Feed?>> DeleteAll(List<string> userIds, string postId)
    {
        try
        {
            var batches = new List<Task<BatchWriteItemResponse>>();

            for (int i = 0; i < userIds.Count; i += DYNAMO_DB_BATCH_WRITE_LIMIT)
            {
                List<string> ids = userIds.Skip(i).Take(DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT).ToList();
                var batch = new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        [TABLE_NAME] = ids.Select(id => new WriteRequest
                        {
                            DeleteRequest = new DeleteRequest
                            {
                                Key = new Dictionary<string, AttributeValue>
                                {
                                    ["UserId"] = new AttributeValue { S = id },
                                    ["PostId"] = new AttributeValue { S = postId }
                                }
                            }
                        }).ToList()
                    }
                };
                batches.Add(_client.BatchWriteItemAsync(batch));
            }

            await Task.WhenAll(batches);
            
            return ServiceResult<Feed?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Feed?>.Failure(e, $"Failed to delete all for {postId}.", "FeedsService.DeleteAll()");
        }
    }
}
