namespace ChestTracker;

public static class ReminderPolicy
{
    public const int DelaySeconds=15;
    public const int VisibleSeconds=6;
    public static bool Ready(TrackerState state,DateTime utcNow)=>state.SpiritReminder&&!string.IsNullOrEmpty(state.PendingReminder)&&state.LastReminder!=state.PendingReminder&&state.PendingReminderNotBeforeUtc!=default&&utcNow>=state.PendingReminderNotBeforeUtc;
    public static bool Queue(TrackerStore store,string finalizedFileKey,DateTime? utcNow=null)
    {
        if(!store.State.SpiritReminder||store.State.ReminderQueuedFiles.Contains(finalizedFileKey))return false;
        store.State.ReminderQueuedFiles.Add(finalizedFileKey);
        store.State.PendingReminder=finalizedFileKey;
        store.State.PendingReminderNotBeforeUtc=(utcNow??DateTime.UtcNow).AddSeconds(DelaySeconds);
        return true;
    }
}
