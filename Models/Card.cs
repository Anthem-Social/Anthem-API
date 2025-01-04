namespace AnthemAPI.Models;

public class Card
{
    public required string UserId { get; set; }
    public required string Nickname { get; set; }
    public string? PictureUrl { get; set; }
}
