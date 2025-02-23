using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("StatusConnections")]
public class StatusConnection
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required HashSet<string> ConnectionIds { get; set; }
}

public class StatusConnectionCreate
{
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
}
