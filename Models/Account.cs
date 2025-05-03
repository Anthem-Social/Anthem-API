using AnthemAPI.Common;

public class Account {
    public required string Id { get; set; }
    public required MusicProvider MusicProvider { get; set; }
    public string? PictureUrl { get; set; }
}
