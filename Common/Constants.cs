namespace AnthemAPI.Common;

public record PollingTier(int IntervalSeconds, string Group);

public static class Constants
{
    public const string ChatCreator = "ChatCreator";
    public const string ChatMember = "ChatMember";
    public const string CommentCreator = "CommentCreator";
    public const string LikeCreator = "LikeCreator";
    public const string MessageCreator = "MessageCreator";
    public const string PostCreator = "PostCreator";
    public const string Self = "Self";
    public const string Spotify = "Spotify";
    public const int DYNAMO_DB_BATCH_GET_LIMIT = 100;
    public const int DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT = 25;
    public const int DYNAMO_DB_BATCH_WRITE_LIMIT = 25;
    public const int FEEDS_TTL_DAYS = 30;
    public static readonly PollingTier Active = new PollingTier(15, "Active");
    public static readonly PollingTier Reduced = new PollingTier(120, "Reduced");
}
