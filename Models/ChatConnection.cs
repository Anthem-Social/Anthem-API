using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("ChatConnections")]
public class ChatConnection
{
    [DynamoDBHashKey]
    public required string ChatId { get; set; }
    public required HashSet<string> ConnectionIds { get; set; }
}

public class ChatConnectionCreate
{
    public required string ConnectionId { get; set; }
    public required string ChatId { get; set; }
}
