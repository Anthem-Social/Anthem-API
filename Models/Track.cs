namespace AnthemAPI.Models;

public class Track
{
    public required Album Album { get; set; }
    public required List<Artist> Artists { get; set; }
    public string? PreviewUrl { get; set; }
    public required string Name { get; set; }
    public required string Uri { get; set; }
}
