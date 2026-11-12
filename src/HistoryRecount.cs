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
            available.TryGetValue(Path.GetFileName(old.File ?? ""), out var file);
            var request = Request(old, file);
            if (store.State.RecountAttempts.GetValueOrDefault(request.AttemptKey) != request.Stamp) return request;
        }
        return null;
    }
    public static string Stamp(FileInfo file) => $"{file.Name}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";

    public static RecountRequest Request(MatchEntry match, FileInfo? file) =>
        new(match, file, $"{CounterSchema.Version}|{match.Profile}|{match.Id}", file == null ? "missing:" + match.File : Stamp(file));

    public static bool Apply(TrackerStore store,RecountRequest request,MatchEntry fresh, bool manual = false)
    {
        if(fresh.Id!=request.Match.Id||fresh.Profile!=request.Match.Profile)
            throw new InvalidDataException("Повтор не совпадает с матчем и владельцем в истории");
        int index=store.State.Matches.IndexOf(request.Match);if(index<0)return false;
        string backup = Path.Combine(store.Folder, manual ? "history-before-manual-recount.json" : "history-before-5.0.json");
        if(File.Exists(store.PathName)&&!File.Exists(backup))File.Copy(store.PathName,backup,false);
        fresh.CounterVersion = CounterSchema.Version;
        fresh.RecountStatus = manual ? "Пересчитано вручную" : "Пересчитано в 5.0";
        store.State.Matches[index] = fresh;
        if (manual)
        {
            try { store.Save(); }
            catch
            {
                store.State.Matches[index] = request.Match;
                throw;
            }
        }
        else store.State.RecountAttempts[request.AttemptKey] = request.Stamp;
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
