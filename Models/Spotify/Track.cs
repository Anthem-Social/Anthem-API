namespace AnthemAPI.Models;

public class Track
{
    public required string Uri { get; set; }
    public required string Name { get; set; }
    public required List<Artist> Artists { get; set; }
    public required Album Album { get; set; }
}
