using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("ChatConnections")]
public class ChatConnection
{
    [DynamoDBHashKey]
    public required string ChatId { get; set; }
    public required HashSet<string> ConnectionIds { get; set; }
}
