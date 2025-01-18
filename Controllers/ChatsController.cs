using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("chats")]
public class ChatsController
(
    ChatConnectionService chatConnectionService,
    ChatService chatService,
    MessageService messageService,
    UserService userService
) : ControllerBase
{
    private readonly ChatConnectionService _chatConnectionService = chatConnectionService;
    private readonly ChatService _chatService = chatService;
    private readonly MessageService _messageService = messageService;
    private readonly UserService _userService = userService;

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

        var save = await _chatService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);

        // Add the Chat Id to the Members' ChatIds Lists
        var add = await _userService.AddChatIdToMembers(chat.UserIds.ToList(), chat.Id);

        if (add.IsFailure)
            return StatusCode(500);
        
        return Created();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var load = await _chatService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        return Ok(load.Data);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _chatService.Delete(id);
        
        if (result.IsFailure)
            return StatusCode(500);

        return NoContent();
    }

    // [HttpPost("{id}/connection/{connectionId}")]
    [HttpPost("connect")]
    public async void CreateConnection([FromBody] ChatConnectionCreate connection)
    {
        Console.WriteLine("Adding Connection " + connection.Id);
        await _chatConnectionService.AddConnectionId(connection.ChatId, connection.Id);
    }

    // [HttpDelete("{id}/connection/{connectionId}")]
    [HttpPost("disconnect")]
    public async void DeleteConnection([FromBody] ChatConnectionCreate connection)
    {
        Console.WriteLine("Deleting Connection " + connection.Id);
        await _chatConnectionService.RemoveConnectionId(connection.ChatId, connection.Id);
    }

    [HttpPost("{id}/members/{userId}")]
    public async Task<IActionResult> CreateMember(string id, string userId)
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
    public async Task<IActionResult> DeleteMember(string id, string userId)
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

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> CreateMessage(string id, [FromBody] MessageCreate dto)
    {
        // TODO: send to live chatters
        var now = DateTime.UtcNow;
        var message = new Message
        {
            ChatId = id,
            Id = $"{now:o}#{dto.UserId}",
            UserId = dto.UserId,
            CreatedAt = now,
            ContentType = dto.ContentType,
            Content = dto.Content
        };

        var save = await _messageService.Save(message);

        if (save.IsFailure)
            return StatusCode(500);
        
        // Update the Chat
        var update = await _chatService.Update(id, message.CreatedAt, message.Content);

        if (update.IsFailure)
            return StatusCode(500);

        return Created();
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(string id, [FromQuery] int page = 1)
    {
        var load = await _messageService.LoadBatch(id, page);

        if (load.IsFailure)
            return StatusCode(500);
        
        return Ok(load.Data);
    }

    [HttpDelete("{id}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(string id, string messageId)
    {
        var delete = await _messageService.Delete(id, messageId);

        if (delete.Data is null)
            return NotFound();
        
        if (delete.IsFailure)
            return StatusCode(500);
        
        return NoContent();
    }

    [HttpPatch("{id}/name")]
    public async Task<IActionResult> Rename(string id, string name)
    {
        // Load the Chat
        var load = await _chatService.Load(id);

        if (load.IsFailure)
            return StatusCode(500);
        
        if (load.Data is null)
            return NotFound();
        
        // Update the name
        Chat chat = load.Data;
        chat.Name = name;

        // Save the Chat
        var save = await _chatService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);

        return Ok(chat);
    }
}
