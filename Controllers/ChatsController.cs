using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("chats")]
public class ChatsController
(
    ChatService chatService,
    MessageService messageService,
    UserService userService
) : ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly MessageService _messageService = messageService;
    private readonly UserService _userService = userService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var load = await _chatService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);
        
        return Ok(load.Data);
    }

    [HttpGet("cards/{userId}")]
    public async Task<IActionResult> GetCards(string userId, [FromQuery] int page = 0)
    {
        return Ok();
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(string id, [FromQuery] int page = 0)
    {
        var load = await _messageService.LoadBatch(id, page);

        if (load.IsFailure)
            return StatusCode(500);
        
        return Ok(load.Data);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> CreateMessage(string id, [FromBody] MessageCreate dto)
    {
        var message = new Message
        {
            ChatId = id,
            UserId = dto.UserId,
            SentAt = DateTime.UtcNow,
            ContentType = dto.ContentType,
            Content = dto.Content
        };

        var save = await _messageService.Save(message);

        if (save.IsFailure)
            return StatusCode(500); 

        return Ok(message);
    }

    [HttpPost("{id}/members/{userId}")]
    public async Task<IActionResult> AddMember(string id, string userId)
    {
        // Load the Chat
        var loadChat = await _chatService.Load(id);

        if (loadChat.IsFailure)
            return StatusCode(500);

        if (loadChat.Data is null)
            return NotFound($"No Chat with Id: {id}");

        // Load the User
        var loadUser = await _userService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound($"No User with Id: {userId}");
        
        // Add the UserId to the Chat members list
        Chat chat = loadChat.Data;
        bool addedUser = chat.UserIds.Add(userId);
        
        if (addedUser) // Save the Chat
        {        
            var saveChat = await _chatService.Save(chat);

            if (saveChat.IsFailure)
                return StatusCode(500);
        }
        
        // Add the ChatId to the User's chats list
        User user = loadUser.Data;
        bool addedChat = user.ChatIds.Add(id);
        
        if (addedChat) // Save the User
        {
            var saveUser = await _userService.Save(user);

            if (saveUser.IsFailure)
                return StatusCode(500);
        }

        return Ok(chat);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        // Load the Chat
        var loadChat = await _chatService.Load(id);

        if (loadChat.IsFailure)
            return StatusCode(500);

        if (loadChat.Data is null)
            return NotFound($"No Chat with Id: {id}");

        // Load the User
        var loadUser = await _userService.Load(userId);

        if (loadUser.IsFailure)
            return StatusCode(500);

        if (loadUser.Data is null)
            return NotFound($"No User with Id: {userId}");
        
        // Remove the UserId from the Chat members list
        Chat chat = loadChat.Data;
        bool removedUser = chat.UserIds.Remove(userId);
        
        if (removedUser) // Save the Chat
        {        
            var saveChat = await _chatService.Save(chat);

            if (saveChat.IsFailure)
                return StatusCode(500);
        }
        
        // Remove the ChatId from the User's chats list
        User user = loadUser.Data;
        bool removedChat = user.ChatIds.Remove(id);
        
        if (removedChat) // Save the User
        {
            var saveUser = await _userService.Save(user);

            if (saveUser.IsFailure)
                return StatusCode(500);
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatCreate dto)
    {
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

        var save = await _chatService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);
                
        return CreatedAtAction(nameof(Get), new { id = chat.Id }, chat);
    }

    [HttpPatch("{id}/name")]
    public async Task<IActionResult> Rename(string id, string name)
    {
        var load = await _chatService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        Chat chat = load.Data;

        chat.Name = name;

        var save = await _chatService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);

        return Ok(chat);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _chatService.Delete(id);
        
        if (result.IsFailure)
            return StatusCode(500);

        return NoContent();
    }
}
