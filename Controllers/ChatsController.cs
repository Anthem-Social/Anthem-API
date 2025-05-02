using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;
using AnthemAPI.Common;
using Microsoft.AspNetCore.Authorization;
using static AnthemAPI.Common.Constants;
using System.Security.Claims;

[ApiController]
[Route("chats")]
public class ChatsController
(
    ChatConnectionsService chatConnectionsService,
    ChatsService chatsService,
    IConfiguration configuration,
    FollowersService followersService,
    MessagesService messagesService,
    UsersService usersService
) : ControllerBase
{
    private readonly ChatConnectionsService _chatConnectionsService = chatConnectionsService;
    private readonly ChatsService _chatsService = chatsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly FollowersService _followersService = followersService;
    private readonly MessagesService _messagesService = messagesService;
    private readonly UsersService _usersService = usersService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatCreate dto)
    {
        // Get the creator user
        string userId = User.FindFirstValue("user_id")!;
        
        // Ensure the members are the user's friends
        var loadFriends = await _followersService.LoadFriends(userId);

        if (loadFriends.IsFailure || loadFriends.Data is null)
            return StatusCode(500);
        
        HashSet<string> friends = loadFriends.Data.Select(f => f.FollowerUserId).ToHashSet();

        if (!dto.UserIds.IsSubsetOf(friends))
            return StatusCode(500);

        // Create the new Chat
        var chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            UserIds = dto.UserIds,
            LastMessageAt = DateTime.UtcNow,
            Preview = "New chat created. Start the conversation!",
            CreatorUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var save = await _chatsService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);

        // Add the Chat Id to the Members' ChatIds Lists
        var add = await _usersService.AddChatIdToAll(chat.UserIds.ToList(), chat.Id);

        if (add.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [Authorize(ChatMember)]
    [HttpGet("{chatId}")]
    public async Task<IActionResult> Get(string chatId)
    {
        var load = await _chatsService.Load(chatId);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        return Ok(load.Data);
    }

    [Authorize(ChatCreator)]
    [HttpDelete("{chatId}")]
    public async Task<IActionResult> Delete(string chatId)
    {
        // Load the Chat
        var loadChat = await _chatsService.Load(chatId);

        if (loadChat.IsFailure)
            return StatusCode(500);
        
        if (loadChat.Data is null)
            return NotFound();
        
        Chat chat = loadChat.Data;

        // Delete the Chat
        var deleteChat = await _chatsService.Delete(chatId);
        
        if (deleteChat.IsFailure)
            return StatusCode(500);
        
        // Delete the ChatId from the Users lists
        var deleteChatIds = await _usersService.RemoveChatIdFromAll(chat.UserIds.ToList(), chat.Id);

        if (deleteChatIds.IsFailure)
            return StatusCode(500);

        return NoContent();
    }

    [Authorize(ChatMember)]
    [HttpPost("{chatId}/members/{userId}")]
    public async Task<IActionResult> CreateMember(string chatId, string userId)
    {
        // Load the Chat
        var loadChat = await _chatsService.Load(chatId);

        if (loadChat.IsFailure)
            return StatusCode(500);

        if (loadChat.Data is null)
            return NotFound($"No Chat with Id: {chatId}");

        // Load the User
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound($"No User with Id: {userId}");
        
        // Add the UserId to the Chat members list
        Chat chat = loadChat.Data;
        bool addedUser = chat.UserIds.Add(userId);
        
        if (addedUser) // Save the Chat
        {        
            var saveChat = await _chatsService.Save(chat);

            if (saveChat.IsFailure)
                return StatusCode(500);
        }
        
        // Add the ChatId to the User's chats list
        // User user = loadUser.Data;
        // bool addedChat = user.ChatIds.Add(chatId);
        
        // if (addedChat) // Save the User
        // {
        //     var saveUser = await _usersService.Save(user);

        //     if (saveUser.IsFailure)
        //         return StatusCode(500);
        // }

        return Ok(chat);
    }

    [Authorize(ChatMember)]
    [Authorize(Self)]
    [HttpDelete("{chatId}/members/{userId}")]
    public async Task<IActionResult> DeleteMember(string chatId, string userId)
    {
        // Load the Chat
        var loadChat = await _chatsService.Load(chatId);

        if (loadChat.IsFailure)
            return StatusCode(500);

        if (loadChat.Data is null)
            return NotFound($"No Chat with Id: {chatId}");

        // Load the User
        var loadUser = await _usersService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound($"No User with Id: {userId}");
        
        // Remove the UserId from the Chat members list
        Chat chat = loadChat.Data;
        bool removedUser = chat.UserIds.Remove(userId);
        
        if (removedUser) // Save the Chat
        {        
            var saveChat = await _chatsService.Save(chat);

            if (saveChat.IsFailure)
                return StatusCode(500);
        }
        
        // Remove the ChatId from the User's chats list
        // User user = loadUser.Data;
        // bool removedChat = user.ChatIds.Remove(chatId);
        
        // if (removedChat) // Save the User
        // {
        //     var saveUser = await _usersService.Save(user);

        //     if (saveUser.IsFailure)
        //         return StatusCode(500);
        // }

        return NoContent();
    }

    [Authorize(ChatMember)]
    [HttpPost("{chatId}/messages")]
    public async Task<IActionResult> CreateMessage(string chatId, [FromBody] MessageCreate dto)
    {
        // Make the new Message
        var now = DateTime.UtcNow;
        string userId = User.FindFirstValue("user_id")!;
        var message = new Message
        {
            ChatId = chatId,
            Id = $"{now:o}#{userId}",
            ContentType = dto.ContentType,
            Content = dto.Content
        };

        var save = await _messagesService.Save(message);

        if (save.IsFailure)
            return StatusCode(500);
        
        // Send Message to live chat connections
        var load = await _chatConnectionsService.Load(chatId);

        if (load.IsSuccess && load.Data is not null && load.Data.ConnectionIds.Count > 0)
        {
            List<string> gone = await Utility.SendToConnections(_configuration["ChatApiGatewayUrl"]!, load.Data.ConnectionIds, message);

            if (gone.Count > 0)
            {
                // Remove gone connections
                await _chatConnectionsService.RemoveConnections(chatId, gone);
            }
        }

        // Update the Chat
        var update = await _chatsService.Update(chatId, now, message.Content);

        if (update.IsFailure)
            return StatusCode(500);

        return Created();
    }

    [Authorize(ChatMember)]
    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(string chatId, [FromQuery] string? exclusiveStartKey = null)
    {
        var load = await _messagesService.LoadPage(chatId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new
        {
            messages = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

    [Authorize(ChatMember)]
    [Authorize(MessageCreator)]
    [HttpDelete("{chatId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(string chatId, string messageId)
    {
        var delete = await _messagesService.Delete(chatId, messageId);

        if (delete.Data is null)
            return NotFound();
        
        if (delete.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [Authorize(ChatMember)]
    [HttpPatch("{chatId}/name")]
    public async Task<IActionResult> Rename(string chatId, [FromBody] string name)
    {
        // Load the Chat
        var load = await _chatsService.Load(chatId);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        // Update the name
        Chat chat = load.Data;
        chat.Name = name;

        // Save the Chat
        var save = await _chatsService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);

        return Ok(chat);
    }
}
