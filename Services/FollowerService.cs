using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class FollowerService
{
    private readonly DynamoDBContext _context;
    public FollowerService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
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
            return ServiceResult<Follower>.Failure(e, $"Failed to save for {follower.UserId} and {follower.FollowerUserId}.", "FollowerService.Save()");
        }
    }

    public async Task<ServiceResult<Follower?>> Delete(string userId, string followerUserId)
    {
        try
        {
            var follower = await _context.LoadAsync<Follower>(userId, followerUserId);

            if (follower is not null)
                await _context.DeleteAsync(follower);
            
            return ServiceResult<Follower?>.Success(follower);
        }
        catch (Exception e)
        {
            return ServiceResult<Follower?>.Failure(e, $"Failed to delete for {userId} and {followerUserId}.", "FollowerService.Delete()");
        }
    }

    public async Task<ServiceResult<List<Follower>?>> LoadFollowers(string userId, int page)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "UserId = :userId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":userId", userId }
                    }
                },
                Limit = FOLLOW_BATCH_LIMIT
            };

            var search = _context.FromQueryAsync<Follower>(query);

            var followers = new List<Follower>();

            for (int x = 0; x < page; x++)
            {
                followers = await search.GetNextSetAsync();
            }

            return ServiceResult<List<Follower>?>.Success(followers);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>?>.Failure(e, $"Failed to load for {userId}.", "FollowerService.LoadFollowers()");
        }
    }

    public async Task<ServiceResult<List<Follower>?>> LoadFollowing(string followerUserId, int page)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                IndexName = "Follower-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "FollowerUserId = :followerUserId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":followerUserId", followerUserId }
                    }
                },
                Limit = FOLLOW_BATCH_LIMIT
            };

            var search = _context.FromQueryAsync<Follower>(query);

            var following = new List<Follower>();

            for (int x = 0; x < page; x++)
            {
                following = await search.GetNextSetAsync();
            }

            return ServiceResult<List<Follower>?>.Success(following);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>?>.Failure(e, $"Failed to load for {followerUserId}.", "FollowerService.LoadFollowing()");
        }
    }

    public async Task<ServiceResult<List<Follower>?>> LoadAllFollowing(string followerUserId)
    {
        try
        {
            int page = 1;
            var following = new List<Follower>();

            while (true)
            {
                var load = await LoadFollowing(followerUserId, page);

                if (load.IsFailure)
                    return load;

                if (load.Data is null)
                    break;
                                
                following.AddRange(load.Data);

                if (load.Data.Count != FOLLOW_BATCH_LIMIT)
                    break;

                page++;
            }

            return ServiceResult<List<Follower>?>.Success(following);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follower>?>.Failure(e, $"Failed to load for {followerUserId}.", "FollowerService.LoadAllFollowing()");
        }
    }

    public async Task<ServiceResult<List<Follower>>> GetMutuals(string followerUserId, List<string> followees)
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
            return ServiceResult<List<Follower>>.Failure(e, "Failed to load batch.", "FollowerService.LoadFriendsBatch()");
        }
    }
}
