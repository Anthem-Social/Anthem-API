using Microsoft.AspNetCore.Mvc;
using AnthemAPI.Services;
using AnthemAPI.Models;

[ApiController]
[Route("chat")]
public class ChatController
(
    ChatService chatService,
    UserChatService userChatService
) : ControllerBase
{
    private readonly ChatService _chatService = chatService;
    private readonly UserChatService _userChatService = userChatService;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _chatService.Load(id);

        if (result.IsFailure)
            return StatusCode(500);
        
        return Ok(result.Data);
    }

    [HttpGet("{userId}/page/{page}")]
    public async Task<IActionResult> GetCards(string userId, int page)
    {
        var userChatsResult = await _userChatService.LoadBatch(userId, page);
        if (userChatsResult.IsFailure)
            return StatusCode(500);

        List<UserChat> userChats = userChatsResult.Data!;
        if (userChats.Count == 0)
            return NoContent();

        List<string> ids = userChats.Select(x => x.ChatId).ToList();

        var chatsResult = await _chatService.GetBatch(ids);
        if (chatsResult.IsFailure)
            return StatusCode(500);

        List<Chat> chats = chatsResult.Data!;

        if (userChats.Count != chats.Count)
            return StatusCode(500);

        List<ChatCard> cards = userChats
            .Zip(chats, (u, c) =>
            {
                if (u.ChatId != c.Id)
                    throw new InvalidOperationException($"ChatId mismatch: {u.ChatId} != {c.Id}");
                    
                return new ChatCard
                {
                    ChatId = u.ChatId,
                    Name = c.Name,
                    UserIds = c.UserIds,
                    Preview = u.Preview,
                    LastMessageAt = u.LastMessageAt,
                    CreatorUserId = c.CreatorUserId,
                    CreatedAt = c.CreatedAt
                };
            })
            .ToList();
        
        return Ok(cards);
    }

    [HttpPut("{id}/member/{userId}")]
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

        var userChat = new UserChat
        {
            UserId = userId,
            ChatId = id,
            LastMessageAt = DateTime.UtcNow,
            Preview = ""
        };
        
        var userChatSave = await _userChatService.Save(userChat);

        if (userChatSave.IsFailure)
            return StatusCode(500);
        
        return Ok(chat);
    }

    [HttpDelete("{id}/member/{userId}")]
    public async Task<IActionResult> RemoveMember(string id, string userId)
    {
        var userChatLoad = await _userChatService.Load(userId, id);

        if (userChatLoad.IsFailure)
            return StatusCode(500);

        if (userChatLoad.Data is null)
            return NotFound($"No UserChat for userId: {userId}, chatId: {id}");

        var userChatDelete = await _userChatService.Delete(userChatLoad.Data);

        if (userChatDelete.IsFailure)
            return StatusCode(500);

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
    public async Task<IActionResult> Create([FromBody] ChatCreate create)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid().ToString(),
            Name = create.Name,
            UserIds = create.UserIds,
            CreatorUserId = create.CreatorUserId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _chatService.Save(chat);

        if (result.IsFailure)
            return StatusCode(500);

        return CreatedAtAction(nameof(Get), new { id = chat.Id }, chat);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Chat chat)
    {
        var result = await _chatService.Save(chat);

        if (result.IsFailure)
            return StatusCode(500);

        return Ok(chat);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _chatService.Delete(id);
        
        if (result.IsFailure)
            return StatusCode(500);

        return NoContent();
    }
}
