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
            return ServiceResult<User?>.Failure($"Failed to load user {id}.\nError: {e}", "UserService.Load()");
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
            return ServiceResult<User>.Failure($"Failed to save user {user.Id}.\nError: {e}", "UserService.Save()");
        }
    }

    public async Task<ServiceResult<User>> AddFollower(string id, string userId)
    {
        try
        {
            var user = await _context.LoadAsync<User>(id);
            user.Followers.Add(userId);
            await _context.SaveAsync(user);
            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure($"Failed to add follower {userId} to {id}.\nError: {e}", "UserService.AddFollower()");
        }
    }

    public async Task<ServiceResult<User>> AddFollowing(string id, string userId)
    {
        try
        {
            var user = await _context.LoadAsync<User>(id);
            user.Following.Add(userId);
            await _context.SaveAsync(user);
            return ServiceResult<User>.Success(user);
        }
        catch (Exception e)
        {
            return ServiceResult<User>.Failure($"Failed to add following {userId} to {id}.\nError: {e}", "UserService.AddFollowing()");
        }
    }
}