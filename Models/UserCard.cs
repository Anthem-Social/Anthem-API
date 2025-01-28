namespace AnthemAPI.Models;

public class UserCard
{
    public required string UserId { get; set; }
    public required string Nickname { get; set; }
    public string? PictureUrl { get; set; }
}
