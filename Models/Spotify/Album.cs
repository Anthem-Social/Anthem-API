namespace AnthemAPI.Models;

public class Album
{
    public required List<Artist> Artists { get; set; }
    public required string ImageUrl { get; set; }
    public required string Name { get; set; }
    public required string Uri { get; set; }
}
