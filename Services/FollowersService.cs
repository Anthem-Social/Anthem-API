using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AnthemAPI.Common;
using AnthemAPI.Common.Helpers;
using AnthemAPI.Models;

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
                    CreatedAt = Helpers.ToDateTimeUTC(follower["CreatedAt"].S)
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
                    CreatedAt = Helpers.ToDateTimeUTC(following["CreatedAt"].S)
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

    public async Task<ServiceResult<List<Follower>>> LoadAllFollowings(string followerUserId)
    {
        try
        {
            string? exclusiveStartKey = null;
            var followings = new List<Follower>();

            while (true)
            {
                var load = await LoadPageFollowings(followerUserId, exclusiveStartKey);

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
            return ServiceResult<List<Follower>>.Failure(e, $"Failed to load for {followerUserId}.", "FollowersService.LoadAllFollowings()");
        }
    }

    public async Task<ServiceResult<List<Follower>>> LoadFriends(string followerUserId, List<string> followees)
    {
        try
        {
            var batch = _context.CreateBatchGet<Follower>();
            followees.ForEach(followee => batch.AddKey(followerUserId, followee));
            await batch.ExecuteAsync();
            return ServiceResult<List<Follower>>.Success(batch.Results);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>>.Failure(e, $"Failed to load for {followerUserId}.", "FollowersService.LoadFriends()");
        }
    }
}
