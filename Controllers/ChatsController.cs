using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;
using AnthemAPI.Common;

[ApiController]
[Route("chats")]
public class ChatsController
(
    ChatConnectionsService chatConnectionsService,
    ChatsService chatsService,
    IConfiguration configuration,
    MessagesService messagesService,
    UsersService usersService
) : ControllerBase
{
    private readonly ChatConnectionsService _chatConnectionsService = chatConnectionsService;
    private readonly ChatsService _chatsService = chatsService;
    private readonly IConfiguration _configuration = configuration;
    private readonly MessagesService _messagesService = messagesService;
    private readonly UsersService _usersService = usersService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatCreate dto)
    {
        // Create the new Chat
        var chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            UserIds = dto.UserIds,
            LastMessageAt = DateTime.UtcNow,
            Preview = "",
            CreatorUserId = dto.CreatorUserId,
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

    [HttpDelete("{chatId}")]
    public async Task<IActionResult> Delete(string chatId)
    {
        var delete = await _chatsService.Delete(chatId);
        
        if (delete.IsFailure)
            return StatusCode(500);

        return NoContent();
    }

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
        User user = loadUser.Data;
        bool addedChat = user.ChatIds.Add(chatId);
        
        if (addedChat) // Save the User
        {
            var saveUser = await _usersService.Save(user);

            if (saveUser.IsFailure)
                return StatusCode(500);
        }

        return Ok(chat);
    }

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
        User user = loadUser.Data;
        bool removedChat = user.ChatIds.Remove(chatId);
        
        if (removedChat) // Save the User
        {
            var saveUser = await _usersService.Save(user);

            if (saveUser.IsFailure)
                return StatusCode(500);
        }

        return NoContent();
    }

    [HttpPost("{chatId}/messages")]
    public async Task<IActionResult> CreateMessage(string chatId, [FromBody] MessageCreate dto)
    {
        // Make the new Message
        var now = DateTime.UtcNow;
        var message = new Message
        {
            ChatId = chatId,
            Id = $"{now:o}#{dto.UserId}",
            ContentType = dto.ContentType,
            Content = dto.Content
        };

        var save = await _messagesService.Save(message);

        if (save.IsFailure)
            return StatusCode(500);
        
        // Send Message to live chat connections
        var load = await _chatConnectionsService.Load(chatId);
        var gone = new List<string>();

        if (load.IsSuccess && load.Data is not null && load.Data.ConnectionIds.Count > 0)
        {
            gone = await Utility.SendToConnections(_configuration["ChatApiGatewayUrl"]!, load.Data.ConnectionIds, message);

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

    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetMessages(string chatId, [FromQuery] string? exclusiveStartKey = null)
    {
        // TODO: Check if chat exists, 404
        var load = await _messagesService.LoadPage(chatId, exclusiveStartKey);

        if (load.IsFailure)
            return StatusCode(500);
        
        var data = new {
            messages = load.Data.Item1,
            lastEvaluatedKey = load.Data.Item2
        };

        return Ok(data);
    }

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

    [HttpPatch("{chatId}/name")]
    public async Task<IActionResult> Rename(string chatId, string name)
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
