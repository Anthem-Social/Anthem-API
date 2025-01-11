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
        if (result.IsFailure) return StatusCode(500);
        return Ok(result.Data);
    }

    [HttpGet("{userId}/page/{page}")]
    public async Task<IActionResult> GetCards(string userId, int page)
    {
        var userChatsResult = await _userChatService.GetBatch(userId, page);
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

        if (result.IsFailure) return StatusCode(500);

        return CreatedAtAction("Get", "Message", new { id = chat.Id }, chat);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Chat chat)
    {
        var result = await _chatService.Save(chat);
        if (result.IsFailure) return StatusCode(500);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _chatService.Delete(id);
        if (result.IsFailure) return StatusCode(500);
        return NoContent();
    }
}
