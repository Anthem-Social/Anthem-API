using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Statuses")]
public class Status
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required List<Artist> Artists { get; set; }
    public required string Track { get; set; }
    public required string TrackUri { get; set; }
    public required string AlbumCoverUrl { get; set; }
    public required string AlbumUri { get; set; }
    public required long LastChanged { get; set; }
}
