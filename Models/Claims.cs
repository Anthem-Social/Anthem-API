namespace AnthemAPI.Models;

public class Claims
{
    public required string Country { get; set; }
    public required bool ExplicitContent { get; set; }
    public required bool Premium { get; set; }
    public required string UserId { get; set; }
}
