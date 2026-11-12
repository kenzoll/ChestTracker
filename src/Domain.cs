using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using ChestCounter;

namespace ChestTracker;

public sealed class ChestEvent
{
    public double Seconds { get; set; }
    public string Category { get; set; } = "";
    public string Blueprint { get; set; } = "";
    public string ActorKey { get; set; } = "";
    public string Mesh { get; set; } = "";
    public bool Counted { get; set; }
    public bool Destroyed { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class MatchEntry
{
    public string Id { get; set; } = "";
    public string Profile { get; set; } = "";
    public string Player { get; set; } = "Игрок";
    public DateTime TimeUtc { get; set; }
    public int Normal { get; set; }
    public int Rare { get; set; }
    public int Ignored { get; set; }
    public int Broken { get; set; }
    public string File { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Source { get; set; } = "Повтор";
    public string GameVersion { get; set; } = "";
    public int CounterVersion { get; set; }
    public string RecountStatus { get; set; } = "";
    public List<ChestEvent> Events { get; set; } = [];
    public int Total => Normal + Rare;
    public string DateLabel => GameClock.Local(TimeUtc).ToString("dd.MM.yyyy  HH:mm");
    public string Categories => $"{Normal} обычных пиратских · {Rare} редких";
}

public sealed class FileResult
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record ManualChestAddition(string Profile, DateTime TimeUtc, long Count);
public sealed record HistoryDay(DateTime Date, MatchEntry[] Matches, long Manual)
{
    public long MatchTotal => Matches.Sum(match => (long)match.Total);
    public long Total => MatchTotal + Manual;
    public string Label => $"{Date.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"))}  ·  {Total:N0}";
}

public sealed class TrackerState
{
    public int Version { get; set; } = 2;
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public string ReplayFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Demos");
    public string ActiveProfile { get; set; } = "";
    public Dictionary<string, long> Baselines { get; set; } = new() { [""] = 0 };
    public List<MatchEntry> Matches { get; set; } = [];
    public HashSet<string> InitialBalancesSet { get; set; } = [];
    public List<ManualChestAddition> ManualAdditions { get; set; } = [];
    public List<SavedRoute> SavedRoutes { get; set; } = [];
    public Dictionary<string, FileResult> Files { get; set; } = [];
    public List<string> Log { get; set; } = [];
    public HashSet<string> LegacyImports { get; set; } = [];
    public bool Animations { get; set; } = true;
    public bool ImportBeforeStart { get; set; }
    public bool SpiritReminder { get; set; } = true;
    public string PendingReminder { get; set; } = "";
    public string LastReminder { get; set; } = "";
    public HashSet<string> ReminderQueuedFiles { get; set; } = [];
    public Dictionary<string,string> RecountAttempts { get; set; } = [];
    public DateTime PendingReminderNotBeforeUtc { get; set; }
    public int TimeEncoding { get; set; } = 2;
    public HashSet<string> FoundRewards { get; set; } = [];
}

public sealed class TrackerStore
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true };
    public string Folder { get; }
    public string PathName => Path.Combine(Folder, "tracker-v2.json");
    public TrackerState State { get; private set; }
    public TrackerStore(string folder)
    {
        Folder = folder;
        Directory.CreateDirectory(folder);
        bool repairClock = false;
        if (System.IO.File.Exists(PathName))
        {
            string text = System.IO.File.ReadAllText(PathName);
            using var doc = JsonDocument.Parse(text);
            repairClock = !doc.RootElement.EnumerateObject().Any(p => p.Name.Equals("TimeEncoding", StringComparison.OrdinalIgnoreCase));
            State = JsonSerializer.Deserialize<TrackerState>(text, Json) ?? throw new InvalidDataException("Файл истории пуст");
        }
        else State = new TrackerState();
        if (State.Version != 2 || State.Baselines.Values.Any(v => v < 0 || v > 1_000_000_000_000) ||
            State.Matches.Any(m => m.Normal < 0 || m.Rare < 0) || State.ManualAdditions.Any(m => m.Count is <= 0 or > 1_000_000_000_000))
            throw new InvalidDataException("Некорректный файл истории. Он сохранён без изменений.");
        if (repairClock)
        {
            int fixedCount = 0;
            foreach(var match in State.Matches.Where(m => m.Source is "Повтор" or "Исправлено"))
            {
                match.TimeUtc = GameClock.RepairV2(match.TimeUtc, TimeZoneInfo.Local);
                fixedCount++;
            }
            State.TimeEncoding = 2;
            Log($"Исправлено время старых повторов: {fixedCount}. Использован часовой пояс Windows: {TimeZoneInfo.Local.Id}.");
            Save();
        }
    }
    public IEnumerable<MatchEntry> Current => State.Matches.Where(m => m.Profile == State.ActiveProfile).OrderByDescending(m => m.TimeUtc);
    public long Baseline => State.Baselines.GetValueOrDefault(State.ActiveProfile);
    public bool HasInitialBalance => Baseline > 0 || State.InitialBalancesSet.Contains(State.ActiveProfile);
    private IEnumerable<ManualChestAddition> CurrentAdditions => State.ManualAdditions.Where(item => item.Profile == State.ActiveProfile);
    public long ManualTotal => CurrentAdditions.Sum(item => item.Count);
    public long Total => Baseline + Current.Sum(m => (long)m.Total) + ManualTotal;

    public HistoryDay[] HistoryDays()
    {
        var matches = Current.ToLookup(match => GameClock.Local(match.TimeUtc).Date);
        var additions = CurrentAdditions.ToLookup(item => GameClock.Local(item.TimeUtc).Date);
        return matches.Select(day => day.Key).Union(additions.Select(day => day.Key)).OrderDescending()
            .Select(date => new HistoryDay(date, matches[date].ToArray(), additions[date].Sum(item => item.Count))).ToArray();
    }

    public void SetInitialBalance(long count)
    {
        if (HasInitialBalance) throw new InvalidOperationException("Начальное количество уже сохранено.");
        if (count is < 0 or > 1_000_000_000_000) throw new ArgumentOutOfRangeException(nameof(count));
        string profile = State.ActiveProfile;
        bool hadEntry = State.Baselines.TryGetValue(profile, out long previous);
        State.Baselines[profile] = count;
        State.InitialBalancesSet.Add(profile);
        try { Save(); }
        catch
        {
            State.InitialBalancesSet.Remove(profile);
            if (hadEntry) State.Baselines[profile] = previous;
            else State.Baselines.Remove(profile);
            throw;
        }
    }

    public void AddManualChests(long count, DateTime? utcNow = null)
    {
        if (count is <= 0 or > 1_000_000_000_000) throw new ArgumentOutOfRangeException(nameof(count));
        _ = checked(Total + count);
        State.ManualAdditions.Add(new(State.ActiveProfile, utcNow ?? DateTime.UtcNow, count));
        try { Save(); }
        catch
        {
            State.ManualAdditions.RemoveAt(State.ManualAdditions.Count - 1);
            throw;
        }
    }
    public void StoreRoute(SavedRoute route)
    {
        if (string.IsNullOrWhiteSpace(route.Name) || route.Name.Length > 80)
            throw new ArgumentException("Введите название от 1 до 80 символов.");
        int index = State.SavedRoutes.FindIndex(saved => saved.Id == route.Id);
        if (index < 0)
        {
            State.SavedRoutes.Add(route);
            try { Save(); }
            catch { State.SavedRoutes.RemoveAt(State.SavedRoutes.Count - 1); throw; }
            return;
        }
        var previous = State.SavedRoutes[index];
        State.SavedRoutes[index] = route;
        try { Save(); }
        catch { State.SavedRoutes[index] = previous; throw; }
    }

    public void RemoveRoute(SavedRoute route)
    {
        int index = State.SavedRoutes.IndexOf(route);
        if (index < 0) return;
        State.SavedRoutes.RemoveAt(index);
        try { Save(); }
        catch { State.SavedRoutes.Insert(index, route); throw; }
    }

    public void Save()
    {
        var temp = PathName + ".tmp";
        using (var f = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(f, State, Json);
            f.Flush(true);
        }
        if (System.IO.File.Exists(PathName)) System.IO.File.Replace(temp, PathName, PathName + ".bak", true);
        else System.IO.File.Move(temp, PathName);
    }
    public void Log(string text)
    {
        State.Log.Add($"{DateTime.Now:dd.MM HH:mm:ss}  {text}");
        if (State.Log.Count > 300) State.Log.RemoveRange(0, State.Log.Count - 300);
    }
    public bool Add(MatchEntry match)
    {
        if (State.Matches.Any(m => m.Id == match.Id && m.Profile == match.Profile)) return false;
        // Carry a legacy/default starting balance to the first positively identified account.
        if (string.IsNullOrEmpty(State.ActiveProfile) && !string.IsNullOrEmpty(match.Profile))
        {
            var balance = State.Baselines.GetValueOrDefault("");
            State.Baselines[match.Profile] = State.Baselines.GetValueOrDefault(match.Profile) + balance;
            State.Baselines[""] = 0;
            if (State.InitialBalancesSet.Remove("")) State.InitialBalancesSet.Add(match.Profile);
            for (int i = 0; i < State.ManualAdditions.Count; i++)
            {
                var addition = State.ManualAdditions[i];
                if (addition.Profile == "") State.ManualAdditions[i] = addition with { Profile = match.Profile };
            }
            foreach (var old in State.Matches.Where(m => m.Profile == "")) old.Profile = match.Profile;
            State.ActiveProfile = match.Profile;
        }
        State.Matches.Add(match);
        Log($"Матч сохранён: {match.Total} сундуков ({match.Normal} обычных / {match.Rare} редких; разрушено {match.Broken})");
        return true;
    }
    public int ImportLegacy(string path)
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("version").GetInt32() != 1) throw new InvalidDataException("Нужен файл state.json из первой версии");
        string digest = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path)));
        if (State.LegacyImports.Contains(digest)) return 0;
        if (State.LegacyImports.Count > 0) throw new InvalidDataException("История версии 1 уже перенесена. Повторный импорт другого файла отключён, чтобы не удвоить начальную сумму.");
        var baseline = root.GetProperty("base").GetInt64();
        if (baseline < 0 || baseline > 1_000_000_000_000) throw new InvalidDataException("Некорректная начальная сумма");
        var imports = new List<MatchEntry>();
        foreach (var item in root.GetProperty("history").EnumerateArray())
        {
            string id = "legacy-" + item.GetProperty("id").GetString();
            if (State.Matches.Any(m => m.Id == id)) continue;
            imports.Add(new MatchEntry { Id = id, Profile = State.ActiveProfile, Player = "Из версии 1",
                Normal = item.GetProperty("normal").GetInt32(), Rare = item.GetProperty("mythic").GetInt32(),
                TimeUtc = DateTime.Parse(item.GetProperty("time").GetString()!).ToUniversalTime(), Source = "Из версии 1" });
        }
        var current = root.GetProperty("current");
        int n = current.GetProperty("normal").GetInt32(), r = current.GetProperty("mythic").GetInt32();
        if (imports.Any(m => m.Normal < 0 || m.Rare < 0) || n < 0 || r < 0) throw new InvalidDataException("Отрицательное число сундуков");
        if (n + r > 0) imports.Add(new MatchEntry { Id = "legacy-current-" + digest, Profile = State.ActiveProfile,
            Player = "Из версии 1", Normal = n, Rare = r, TimeUtc = DateTime.UtcNow, Source = "Незавершённый из v1" });
        State.Baselines[State.ActiveProfile] = Baseline + baseline;
        State.Matches.AddRange(imports);
        State.LegacyImports.Add(digest);
        Log($"Перенесены данные версии 1: {imports.Count} матчей. Старый файл не изменён.");
        Save();
        return imports.Count;
    }
    public string ExportCsv(IEnumerable<MatchEntry> matches)
    {
        string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        var b = new StringBuilder("Дата;Игрок;Обычные пиратские;Редкие;Из них разрушено;За матч;Накоплено;Источник\r\n");
        long total = Baseline;
        foreach (var m in matches.OrderBy(m => m.TimeUtc))
        {
            total += m.Total;
            b.AppendLine($"{Q(m.DateLabel)};{Q(m.Player)};{m.Normal};{m.Rare};{m.Broken};{m.Total};{total};{Q(m.Source)}");
        }
        return b.ToString();
    }
}

public static class ChestFilter
{
    public static string Category(ChestRecord chest)
    {
        var b = chest.Blueprint.ToLowerInvariant();
        // Fail closed. Never infer eligibility just from the word "chest".
        if (new[] { "ammo", "food", "cooler", "barrel", "relic", "extract", "goldenchest", "treasurehandler", "sprite", "spirit", "booty", "buried" }.Any(b.Contains)) return "";
        if (chest.Kind == ChestKinds.Rare) return "rare";
        if (chest.Kind == ChestKinds.Normal) return "normal";
        return "";
    }
    public static MatchEntry Convert(MatchRecord record)
    {
        if (!record.IsReplayOwner || string.IsNullOrWhiteSpace(record.PlayerName))
            throw new InvalidDataException("Не удалось надёжно определить владельца повтора. Матч не учтён.");
        var unique=record.Chests.Where(c=>string.IsNullOrEmpty(c.ActorKey)).Concat(record.Chests.Where(c=>!string.IsNullOrEmpty(c.ActorKey)).GroupBy(c=>c.ActorKey).Select(g=>g.First()));
        var events = unique.Select(c => new ChestEvent { Seconds = c.TimeSeconds, Blueprint = c.Blueprint, ActorKey=c.ActorKey, Mesh=c.Mesh,
            Category = Category(c), Counted = Category(c) != "", Destroyed = c.Destroyed,
            Reason = Category(c) == "" ? "Тип вне фильтра / неизвестен" : c.Destroyed ? "Разрушен владельцем повтора" : "Открыт владельцем повтора" }).ToList();
        return new MatchEntry { Id = record.SessionId, Profile = record.EpicId ?? record.PlayerName, Player = record.PlayerName,
            TimeUtc = GameClock.ReplayUtc(record.MatchStartUtc ?? record.AnalyzedAtUtc), File = record.ReplayFile,
            Mode = record.Playlist ?? "", GameVersion = record.GameVersion ?? "", Events = events, CounterVersion=CounterSchema.Version,
            Normal = events.Count(e => e.Category == "normal"),
            Rare = events.Count(e => e.Category == "rare"), Broken = events.Count(e => e.Counted && e.Destroyed),
            Ignored = events.Count(e => !e.Counted) + record.AmmoBoxesOpened + record.OtherContainersOpened };
    }
}

public sealed record ReplayHeader(bool Valid, bool Live, bool MissingKey, string Error);
public static class ReplayProbe
{
    public static ReplayHeader Read(string path)
    {
        try
        {
            using var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var r = new BinaryReader(f);
            if (r.ReadUInt32() != 0x1CA2E27F) return new(false, false, true, "Не является повтором Fortnite");
            var version = r.ReadUInt32();
            if (version >= 7)
            {
                int count = r.ReadInt32();
                if (count < 0 || count > 64) throw new InvalidDataException("Некорректная таблица заголовка");
                f.Seek(count * 20L, SeekOrigin.Current);
            }
            f.Seek(12, SeekOrigin.Current);
            int len = r.ReadInt32();
            if (Math.Abs((long)len) > 1_000_000) throw new InvalidDataException("Некорректное имя повтора");
            f.Seek(len < 0 ? -2L * len : len, SeekOrigin.Current);
            bool live = r.ReadUInt32() != 0;
            r.ReadInt64(); r.ReadUInt32();
            bool encrypted = version >= 6 && r.ReadUInt32() != 0;
            if (!encrypted) return new(true, live, false, "");
            int keyLen = r.ReadInt32();
            if (keyLen < 0 || keyLen > 256) throw new InvalidDataException("Некорректный ключ повтора");
            byte[] key = r.ReadBytes(keyLen);
            return new(true, live, key.Length != keyLen || key.Length == 0 || key.All(b => b == 0), "");
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        { return new(false, false, true, e.Message); }
    }
}

public sealed class AnalysisResult
{
    public string Status { get; set; } = "error";
    public string Message { get; set; } = "";
    public MatchEntry? Match { get; set; }
    public long ElapsedMs { get; set; }
    public long PeakMemoryMb { get; set; }
}
