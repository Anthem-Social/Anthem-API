namespace AnthemAPI.Models;
public class Profile
{
    public required User User { get; set; }
    public required Resource Anthem { get; set; }
    public required string Bio { get; set; }
    public required DateTime Joined { get; set; }
    public required List<User> Followers { get; set; }
    public required List<User> Following { get; set; }
}