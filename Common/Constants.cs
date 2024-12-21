namespace AnthemAPI.Common;

public record PollingTier(int IntervalSeconds, string Group);

public static class Constants
{
    public const int DYNAMO_DB_BATCH_SIZE = 25;
    public static readonly PollingTier Active = new PollingTier(15, "Active");
    public static readonly PollingTier Reduced = new PollingTier(120, "Reduced");
}
