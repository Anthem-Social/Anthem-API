using AnthemAPI.Enums;

namespace AnthemAPI.Models;

public class Resource
{
    public required ResourceType Type { get; set; }
    public required string URI { get; set; }
}