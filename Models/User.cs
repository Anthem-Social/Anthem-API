namespace AnthemAPI.Models;

public class User
{
    public required string UserId { get; set; }
    public string? Alias { get; set; }
    public string? PictureUrl { get; set; }
    public required DateTime LastActive { get; set; }
    public required Resource LastTrack { get; set; }
}