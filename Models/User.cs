namespace AnthemAPI.Models;

public class User
{
    public required string UserId { get; set; }
    public required string Alias { get; set; }
    public required string ProfilePictureURL { get; set; }
    public required DateTime LastActive { get; set; }
    public required Resource LastTrack { get; set; }
}