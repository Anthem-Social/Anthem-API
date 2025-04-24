using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class FollowersService
{
    private readonly IAmazonDynamoDB _client;
    private readonly DynamoDBContext _context;
    private const int PAGE_LIMIT = 20;
    private const string TABLE_NAME = "Followers";

    public FollowersService(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<Follower?>> Delete(string userId, string followerUserId)
    {
        try
        {
            await _context.DeleteAsync<Follower>(userId, followerUserId);
            return ServiceResult<Follower?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower?>.Failure(e, $"Failed to delete for {userId} and {followerUserId}.", "FollowersService.Delete()");
        }
    }

    public async Task<ServiceResult<Follower?>> DeleteAllFollowers(string userId)
    {
        try
        {
            var search = _context.QueryAsync<Follower>(userId);
            var followers = await search.GetRemainingAsync();
            var batch = _context.CreateBatchWrite<Follower>();
            batch.AddDeleteItems(followers);
            await batch.ExecuteAsync();
            return ServiceResult<Follower?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower?>.Failure(e, $"Failed to delete all followers for {userId}.", "FollowersService.DeleteAllFollowers()");
        }
    }

    public async Task<ServiceResult<Follower?>> DeleteAllFollowings(string userId)
    {
        try
        {
            var search = _context.QueryAsync<Follower>(userId, new DynamoDBOperationConfig
            {
                IndexName = "Follower-index"
            });
            var followings = await search.GetRemainingAsync();
            var batch = _context.CreateBatchWrite<Follower>();
            batch.AddDeleteItems(followings);
            await batch.ExecuteAsync();
            return ServiceResult<Follower?>.Success(null);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower?>.Failure(e, $"Failed to delete all followings for {userId}.", "FollowersService.DeleteAllFollowings()");
        }
    }

    public async Task<ServiceResult<Follower?>> Load(string userId, string followerUserId)
    {
        try
        {
            Follower? follower = await _context.LoadAsync<Follower>(userId, followerUserId);
            return ServiceResult<Follower?>.Success(follower);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower?>.Failure(e, $"Failed for {userId} and {followerUserId}.", "FollowersService.Load()");
        }
    }

    public async Task<ServiceResult<List<Follower>>> LoadAllFollowers(string userId)
    {
        try
        {
            var search = _context.QueryAsync<Follower>(userId);
            List<Follower> followers = await search.GetRemainingAsync();
            return ServiceResult<List<Follower>>.Success(followers);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>>.Failure(e, $"Failed to load all followers for {userId}.", "FollowersService.LoadAllFollowers()");
        }
    }

    public async Task<ServiceResult<Follower>> Save(Follower follower)
    {
        try
        {
            await _context.SaveAsync(follower);
            return ServiceResult<Follower>.Success(follower);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower>.Failure(e, $"Failed to save for {follower.UserId} and {follower.FollowerUserId}.", "FollowersService.Save()");
        }
    }

    public async Task<ServiceResult<(List<Follower>, string?)>> LoadPageFollowers(string userId, string? exclusiveStartKey = null)
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
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["UserId"] = new AttributeValue { S = userId },
                    ["FollowerUserId"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Follower> followers = response.Items
                .Select(follower => new Follower
                {
                    UserId = follower["UserId"].S,
                    FollowerUserId = follower["FollowerUserId"].S,
                    CreatedAt = Utility.ToDateTimeUTC(follower["CreatedAt"].S)
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("FollowerUserId")
                ? response.LastEvaluatedKey["FollowerUserId"].S
                : null;

            return ServiceResult<(List<Follower>, string?)>.Success((followers, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Follower>, string?)>.Failure(e, $"Failed to load page for {userId}.", "FollowersService.LoadPageFollowers()");
        }
    }

    public async Task<ServiceResult<(List<Follower>, string?)>> LoadPageFollowings(string followerUserId, string? exclusiveStartKey = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                IndexName = "Follower-index",
                KeyConditionExpression = "FollowerUserId = :followerUserId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":followerUserId"] = new AttributeValue { S = followerUserId }
                },
                Limit = PAGE_LIMIT
            };

            if (exclusiveStartKey is not null)
            {
                request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    ["FollowerUserId"] = new AttributeValue { S = followerUserId },
                    ["UserId"] = new AttributeValue { S = exclusiveStartKey }
                };
            }
            
            var response = await _client.QueryAsync(request);

            List<Follower> followings = response.Items
                .Select(following => new Follower
                {
                    UserId = following["UserId"].S,
                    FollowerUserId = following["FollowerUserId"].S,
                    CreatedAt = Utility.ToDateTimeUTC(following["CreatedAt"].S)
                })
                .ToList();

            string? lastEvaluatedKey = response.LastEvaluatedKey.ContainsKey("UserId")
                ? response.LastEvaluatedKey["UserId"].S
                : null;

            return ServiceResult<(List<Follower>, string?)>.Success((followings, lastEvaluatedKey));
        }
        catch (Exception e)
        {
            return ServiceResult<(List<Follower>, string?)>.Failure(e, $"Failed to load page for {followerUserId}.", "FollowersService.LoadPageFollowings()");
        }
    }

    public async Task<ServiceResult<List<Follower>>> LoadAllFollowings(string userId)
    {
        try
        {
            string? exclusiveStartKey = null;
            var followings = new List<Follower>();

            while (true)
            {
                var load = await LoadPageFollowings(userId, exclusiveStartKey);

                if (load.IsFailure)
                    return ServiceResult<List<Follower>>.Failure(load.Exception, load.ErrorMessage!, load.ErrorOrigin!);
                
                List<Follower> followingsPage = load.Data.Item1;
                exclusiveStartKey = load.Data.Item2;
                                
                followings.AddRange(followingsPage);

                if (exclusiveStartKey is null)
                    break;
            }

            return ServiceResult<List<Follower>>.Success(followings);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>>.Failure(e, $"Failed to load for {userId}.", "FollowersService.LoadAllFollowings()");
        }
    }

    public async Task<ServiceResult<List<Follower>>> LoadFriends(string userId)
    {
        try
        {
            var followings = await LoadAllFollowings(userId);

            if (followings.IsFailure)
                return ServiceResult<List<Follower>>.Failure(followings.Exception, followings.ErrorMessage!, followings.ErrorOrigin!);

            if (followings.Data is null)
                return ServiceResult<List<Follower>>.Success(new List<Follower>());
            
            List<string> followeeUserIds = followings.Data.Select(f => f.UserId).ToList();
            var batches = new List<BatchGet<Follower>>();

            for (int i = 0; i < followeeUserIds.Count; i += DYNAMO_DB_BATCH_GET_LIMIT)
            {
                List<string> followees = followeeUserIds.Skip(i).Take(DYNAMO_DB_BATCH_GET_LIMIT).ToList();
                var batch = _context.CreateBatchGet<Follower>();
                followees.ForEach(followee => batch.AddKey(userId, followee));
                batches.Add(batch);
            }

            await _context.ExecuteBatchGetAsync(batches.ToArray());

            List<Follower> friends = batches
                .SelectMany(b => b.Results)
                .ToList();

            return ServiceResult<List<Follower>>.Success(friends);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>>.Failure(e, $"Failed to load for {userId}.", "FollowersService.LoadFriends()");
        }
    }

    public async Task<ServiceResult<Relationship>> LoadRelationship(string userIdA, string userIdB)
    {
        try
        {
            if (userIdA == userIdB)
                return ServiceResult<Relationship>.Success(Relationship.Self);
            
            var load_A_Follows_B = await Load(userIdB, userIdA);
            var load_B_Follows_A = await Load(userIdA, userIdB);

            if (load_A_Follows_B.IsFailure)
                return ServiceResult<Relationship>.Failure(load_A_Follows_B.Exception, load_A_Follows_B.ErrorMessage!, load_A_Follows_B.ErrorOrigin!);
            
            if (load_B_Follows_A.IsFailure)
                return ServiceResult<Relationship>.Failure(load_B_Follows_A.Exception, load_B_Follows_A.ErrorMessage!, load_B_Follows_A.ErrorOrigin!);
            
            bool A_Follows_B = load_A_Follows_B.Data is not null;
            bool B_Follows_A = load_B_Follows_A.Data is not null;

            if (A_Follows_B && B_Follows_A)
                return ServiceResult<Relationship>.Success(Relationship.Mutual);
            else if (!A_Follows_B && !B_Follows_A)
                return ServiceResult<Relationship>.Success(Relationship.None);
            else if (A_Follows_B)
                return ServiceResult<Relationship>.Success(Relationship.A_Follows_B);
            else if (B_Follows_A)
                return ServiceResult<Relationship>.Success(Relationship.B_Follows_A);
            
            return ServiceResult<Relationship>.Failure(null, $"Failed to determine relationship.", "FollowersService.LoadRelationship()");
        }
        catch (Exception e)
        {
            return ServiceResult<Relationship>.Failure(e, $"Failed for A: {userIdA}, B: {userIdB}.", "FollowersService.LoadRelationship()");
        }
    }
}
