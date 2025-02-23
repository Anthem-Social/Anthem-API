namespace AnthemAPI.Models;

public class Card
{
    public required string UserId { get; set; }
    public string? Nickname { get; set; }
    public string? PictureUrl { get; set; }
}
