using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Common;
using AnthemAPI.Models;

namespace AnthemAPI.Services;

public class UserService
{
    private readonly DynamoDBContext _context;

    public UserService(IAmazonDynamoDB client)
    {
        _context = new DynamoDBContext(client);
    }

    public async Task<ServiceResult<User?>> Load(string id)
    {
        try
        {
            var user = await _context.LoadAsync<User>(id);
            return ServiceResult<User?>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User?>.Failure(e, $"Failed to load for {id}.", "UserService.Load()");
        }
    }

    public async Task<ServiceResult<User>> Save(User user)
    {
        try
        {
            await _context.SaveAsync(user);
            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure(e, $"Failed to save for {user.Id}.", "UserService.Save()");
        }
    }

    public async Task<ServiceResult<bool>> AddFollower(string followee, string follower)
    {
        try
        {
            var user = await _context.LoadAsync<User>(followee);
            user.Followers.Add(follower);
            await _context.SaveAsync(user);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to add follower {follower} to {followee}.", "UserService.AddFollower()");
        }
    }

    public async Task<ServiceResult<bool>> AddFollowing(string follower, string followee)
    {
        try
        {
            var user = await _context.LoadAsync<User>(follower);
            user.Following.Add(followee);
            await _context.SaveAsync(user);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception e)
        {
            return ServiceResult<bool>.Failure(e, $"Failed to add following {followee} to {follower}.", "UserService.AddFollowing()");
        }
    }
}
