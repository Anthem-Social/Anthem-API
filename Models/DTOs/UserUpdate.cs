namespace AnthemAPI.Models;

public class UserUpdate
{
    public string? Nickname { get; set; }
    public string? PictureUrl { get; set; }
    public string? Bio { get; set; }
    public Track? Anthem { get; set; }
}
