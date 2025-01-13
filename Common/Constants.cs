namespace AnthemAPI.Common;

public record PollingTier(int IntervalSeconds, string Group);

public static class Constants
{
    public const int CHAT_BATCH_LIMIT = 20;
    public const int DYNAMO_DB_BATCH_GET_ITEM_LIMIT = 100;
    public const int DYNAMO_DB_BATCH_EXECUTE_STATEMENT_LIMIT = 25;
    public const int MESSAGE_BATCH_LIMIT = 40;
    public static readonly PollingTier Active = new PollingTier(15, "Active");
    public static readonly PollingTier Reduced = new PollingTier(120, "Reduced");
}
