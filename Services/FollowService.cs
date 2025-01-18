using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AnthemAPI.Common;
using AnthemAPI.Models;
using static AnthemAPI.Common.Constants;

namespace AnthemAPI.Services;

public class FollowService
{
    private readonly DynamoDBContext _context;
    public FollowService(IAmazonDynamoDB db)
    {
        _context = new DynamoDBContext(db);
    }

    public async Task<ServiceResult<Follow>> Save(Follow follow)
    {
        try
        {
            await _context.SaveAsync(follow);
            return ServiceResult<Follow>.Success(follow);
        }
        catch (Exception e)
        {
            return ServiceResult<Follow>.Failure(e, $"Failed to save for {follow.Followee} and {follow.Follower}.", "FollowService.Save()");
        }
    }

    public async Task<ServiceResult<Follow?>> Delete(string followee, string follower)
    {
        try
        {
            var follow = await _context.LoadAsync<Follow>(followee, follower);

            if (follow is not null)
                await _context.DeleteAsync(follow);
            
            return ServiceResult<Follow?>.Success(follow);
        }
        catch (Exception e)
        {
            return ServiceResult<Follow?>.Failure(e, $"Failed to delete for {followee} and {follower}.", "FollowService.Delete()");
        }
    }

    public async Task<ServiceResult<List<Follow>?>> LoadFollowers(string followee, int page)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Followee = :followee",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":followee", followee }
                    }
                },
                Limit = FOLLOW_BATCH_LIMIT
            };

            var search = _context.FromQueryAsync<Follow>(query);

            var follows = new List<Follow>();

            for (int x = 0; x < page; x++)
            {
                follows = await search.GetNextSetAsync();
            }

            return ServiceResult<List<Follow>?>.Success(follows);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follow>?>.Failure(e, $"Failed to load for {followee}.", "FollowService.LoadFollowers()");
        }
    }

    public async Task<ServiceResult<List<Follow>?>> LoadFollowing(string follower, int page)
    {
        try
        {
            var query = new QueryOperationConfig
            {
                IndexName = "Follower-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Follower = :follower",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":follower", follower }
                    }
                },
                Limit = FOLLOW_BATCH_LIMIT
            };

            var search = _context.FromQueryAsync<Follow>(query);

            var follows = new List<Follow>();

            for (int x = 0; x < page; x++)
            {
                follows = await search.GetNextSetAsync();
            }

            return ServiceResult<List<Follow>?>.Success(follows);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follow>?>.Failure(e, $"Failed to load for {follower}.", "FollowService.LoadFollowing()");
        }
    }

    public async Task<ServiceResult<List<Follow>?>> LoadAllFollowing(string follower)
    {
        try
        {
            int page = 1;
            var following = new List<Follow>();

            while (true)
            {
                var load = await LoadFollowing(follower, page);

                if (load.IsFailure)
                    return load;

                if (load.Data is null)
                    break;
                                
                following.AddRange(load.Data);

                if (load.Data.Count != FOLLOW_BATCH_LIMIT)
                    break;

                page++;
            }

            return ServiceResult<List<Follow>?>.Success(following);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follow>?>.Failure(e, $"Failed to load for {follower}.", "FollowerService.LoadAllFollowing()");
        }
    }

    public async Task<ServiceResult<List<Follow>>> GetMutuals(string follower, List<string> followees)
    {
        try
        {
            var batch = _context.CreateBatchGet<Follow>();
            followees.ForEach(followee => batch.AddKey(followee, follower));
            await batch.ExecuteAsync();
            return ServiceResult<List<Follow>>.Success(batch.Results);
        }
        catch (Exception e)
        {
            return ServiceResult<List<Follow>>.Failure(e, "Failed to load batch.", "FollowService.LoadFriendsBatch()");
        }
    }
}
