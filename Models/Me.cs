namespace AnthemAPI.Models;

public class Me
{
    public required string UserId { get; set; }
    public required bool IsPremium { get; set; }
    public required string Country { get; set; }
}