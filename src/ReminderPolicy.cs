namespace ChestTracker;

public static class ReminderPolicy
{
    public static bool Queue(TrackerStore store,string finalizedFileKey)
    {
        if(!store.State.SpiritReminder||store.State.ReminderQueuedFiles.Contains(finalizedFileKey))return false;
        store.State.ReminderQueuedFiles.Add(finalizedFileKey);
        store.State.PendingReminder=finalizedFileKey;
        return true;
    }
}
