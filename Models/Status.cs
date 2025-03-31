using Amazon.DynamoDBv2.DataModel;

namespace AnthemAPI.Models;

[DynamoDBTable("Statuses")]
public class Status
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required Track Track { get; set; }
    public required long LastChanged { get; set; }
}

public class StatusCard
{
    public required string UserId { get; set; }
    public required Card Card { get; set; }
    public required Status Status { get; set; }
}
