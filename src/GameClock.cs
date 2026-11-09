namespace ChestTracker;

public static class GameClock
{
    // Unreal's FDateTime has UTC ticks but the old parser did not set DateTime.Kind.
    public static DateTime ReplayUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Utc => value,
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
    public static DateTime InZone(DateTime value, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeFromUtc(ReplayUtc(value), zone);
    public static DateTime Local(DateTime value) => InZone(value, TimeZoneInfo.Local);
    public static DateTime RepairV2(DateTime stored, TimeZoneInfo originalZone) => DateTime.SpecifyKind(InZone(stored, originalZone), DateTimeKind.Utc);
    public static string SystemLabel
    {
        get
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return $"Время Windows · UTC{(offset < TimeSpan.Zero ? "−" : "+")}{offset.Duration():hh\\:mm}";
        }
    }
}
