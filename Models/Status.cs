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
