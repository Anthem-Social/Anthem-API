namespace AnthemAPI.Common;

public record PollingTier(int IntervalSeconds, string Group);

public static class Constants
{
    public const int DYNAMO_DB_BATCH_LIMIT = 25;
    public const int MESSAGE_BATCH_LIMIT = 40;
    public const int USER_CHAT_BATCH_LIMIT = 15;
    public static readonly PollingTier Active = new PollingTier(15, "Active");
    public static readonly PollingTier Reduced = new PollingTier(120, "Reduced");
}
