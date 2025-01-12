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
}
