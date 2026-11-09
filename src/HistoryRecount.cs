namespace ChestTracker;

public static class CounterSchema { public const int Version=5; }
public sealed record RecountRequest(MatchEntry Match,FileInfo? File,string AttemptKey,string Stamp);
public static class HistoryRecount
{
    public static RecountRequest? Next(TrackerStore store,IEnumerable<FileInfo> files)
    {
        var available=files.ToDictionary(f=>f.Name,StringComparer.OrdinalIgnoreCase);
        foreach(var old in store.State.Matches.Where(m=>m.CounterVersion<CounterSchema.Version).OrderBy(m=>m.TimeUtc))
        {
            string key=$"{CounterSchema.Version}|{old.Profile}|{old.Id}";
            available.TryGetValue(Path.GetFileName(old.File??""),out var file);
            string stamp=file==null?"missing:"+old.File:$"{file.Name}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
            if(store.State.RecountAttempts.GetValueOrDefault(key)==stamp)continue;
            return new(old,file,key,stamp);
        }
        return null;
    }
    public static bool Apply(TrackerStore store,RecountRequest request,MatchEntry fresh)
    {
        if(fresh.Id!=request.Match.Id||fresh.Profile!=request.Match.Profile)
            throw new InvalidDataException("Повтор не совпадает с матчем и владельцем в истории");
        int index=store.State.Matches.IndexOf(request.Match);if(index<0)return false;
        string backup=Path.Combine(store.Folder,"history-before-5.0.json");
        if(File.Exists(store.PathName)&&!File.Exists(backup))File.Copy(store.PathName,backup,false);
        fresh.CounterVersion=CounterSchema.Version;fresh.RecountStatus="Пересчитано в 5.0";
        store.State.Matches[index]=fresh;store.State.RecountAttempts[request.AttemptKey]=request.Stamp;
        store.Log($"Пересчёт {fresh.DateLabel}: {request.Match.Normal}/{request.Match.Rare} → {fresh.Normal}/{fresh.Rare}");
        return true;
    }
    public static void Failed(TrackerStore store,RecountRequest request,string reason)
    {
        request.Match.RecountStatus=reason;
        store.State.RecountAttempts[request.AttemptKey]=request.Stamp;
        store.Log($"Пересчёт {request.Match.DateLabel}: {reason}. Прежний результат сохранён.");
    }
}
