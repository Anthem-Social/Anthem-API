using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class Resource
{
    public required ResourceType Type { get; set; }
    public required string Name { get; set; }
    public required string Uri { get; set; }
}
