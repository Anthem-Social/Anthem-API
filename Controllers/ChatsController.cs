using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("chats")]
public class ChatsController
(
    ChatService chatService,
    MessageService messageService
) : ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly MessageService _messageService = messageService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _chatService.Load(id);

        if (result.IsFailure)
            return StatusCode(500);
        
        return Ok(result.Data);
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
        var chatLoad = await _chatService.Load(id);

        if (chatLoad.IsFailure)
            return StatusCode(500);

        if (chatLoad.Data is null)
            return NotFound();
        
        Chat chat = chatLoad.Data;
        bool added = chat.UserIds.Add(userId);
        
        if (!added)
            return Ok(chat);

        var chatSave = await _chatService.Save(chat);

        if (chatSave.IsFailure)
            return StatusCode(500);

        return Ok(chat);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        var chatLoad = await _chatService.Load(id);

        if (chatLoad.IsFailure)
            return StatusCode(500);

        if (chatLoad.Data is null)
            return NotFound($"No Chat for chatId: {id}");
        
        Chat chat = chatLoad.Data;
        chat.UserIds.Remove(userId);

        if (chat.UserIds.Count == 0)
            return await Delete(id);

        var save = await _chatService.Save(chat);

        if (save.IsFailure)
            return StatusCode(500);
        
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
            CreatorUserId = dto.CreatorUserId,
            CreatedAt = DateTime.UtcNow
        };

        var chatSave = await _chatService.Save(chat);

        if (chatSave.IsFailure)
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
