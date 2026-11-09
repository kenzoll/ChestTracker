using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
using System.Windows;
using ChestCounter;

namespace ChestTracker;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        L.Set("en");
        if(args.Length==2&&args[0]=="--render-brand"){Brand.WriteAssets(args[1]);return 0;}
        if(args.Length==2&&args[0]=="--apply-update")return AppUpdates.Apply(args[1]);
        if (args.Length == 3 && args[0] == "--analyze")
        {
            var elapsed = Stopwatch.StartNew();
            AnalysisResult result;
            try
            {
                var header = ReplayProbe.Read(args[1]);
                if (!header.Valid) throw new InvalidDataException(header.Error);
                if (header.MissingKey || header.Live)
                    result = new() { Status = "waiting", Message = "Повтор ещё записывается. Ожидаю завершения матча." };
                else
                {
                    var file = new FileInfo(args[1]);
                    if (file.Length > 268_435_456) throw new InvalidDataException("Повтор больше 256 МБ. Пропущен для ограничения нагрузки.");
                    using var input = new FileStream(args[1], FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var snapshot = new MemoryStream();
                    input.CopyTo(snapshot);
                    snapshot.Position = 0;
                    var analyzer = new ReplayAnalyzer();
                    var record = analyzer.Analyze(snapshot, file.Name, null);
                    if (analyzer.IsPartialReplay) throw new InvalidDataException("Повтор неполный: итог не сохранён, чтобы не занизить число.");
                    if (!record.IsBattleRoyale) result = new() { Status = "skipped", Message = "Этот повтор не относится к распознаваемому матчу Battle Royale." };
                    else result = new() { Status = "ready", Match = ChestFilter.Convert(record) };
                }
            }
            catch (Exception e)
            { result = new() { Status = "error", Message = $"{e.GetType().Name}: {e.Message}" }; }
            result.ElapsedMs = elapsed.ElapsedMilliseconds;
            result.PeakMemoryMb = Process.GetCurrentProcess().PeakWorkingSet64 / (1024*1024);
            System.IO.File.WriteAllText(args[2], JsonSerializer.Serialize(result, TrackerStore.Json));
            return result.Status == "error" ? 2 : 0;
        }
        if (args.Length == 2 && args[0] == "--self-test") return SelfTests.Run(args[1]);
        if(args.Length==3&&args[0]=="--recount-history")return RecountHistory(args[1],args[2]);
        if (args.Length == 3 && args[0] == "--verify-replay") return VerifyReplay(args[1], args[2]);
        if (args.Length == 2 && args[0] == "--render-ui") return MainWindow.RenderPreview(Path.GetFullPath(args[1]));
        string folder = StoragePaths.DataDirectory;
        // The QA mode is explicit and completely isolated from the user's history.
        bool preview = args.Length == 2 && args[0] == "--preview";
        if (preview) folder = Path.GetFullPath(args[1]);
        string mutexKey = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(folder)))[..20];
        using var mutex = new Mutex(true, @"Local\ChestTrackerV2-" + mutexKey, out bool first);
        if (!first) { MessageBox.Show("Chest Tracker уже запущен. Откройте его окно на панели задач.", "Chest Tracker"); return 0; }
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.DispatcherUnhandledException += (_, e) =>
            {
                MessageBox.Show("Не удалось выполнить действие:\n" + e.Exception.Message + "\n\nИстория не сброшена.", "Chest Tracker");
                e.Handled = true;
            };
            if (!preview) StoragePaths.MigratePortableV2(folder, AppContext.BaseDirectory);
            var store = new TrackerStore(folder);
            if (preview) DemoData.Seed(store);
            else if (!System.IO.File.Exists(store.PathName))
            {
                foreach (string candidate in new[] {
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","ChestTracker","data","state.json")),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","ChestTracker","data","state.json")) })
                {
                    if (!System.IO.File.Exists(candidate)) continue;
                    try { store.ImportLegacy(candidate); }
                    catch(Exception e) { store.Log("Не удалось перенести старую историю: " + e.Message); }
                    break;
                }
            }
            app.Run(new MainWindow(store, preview));
            return 0;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message + "\n\nИстория: " + folder + "\nРезервная копия: tracker-v2.json.bak.", "Ошибка запуска");
            return 1;
        }
    }
    private static int RecountHistory(string folder,string report)
    {
        try
        {
            var store=new TrackerStore(Path.GetFullPath(folder));
            string balances=JsonSerializer.Serialize(store.State.Baselines);
            int count=store.State.Matches.Count;
            var before=store.State.Matches.Select(m=>new{m.Id,m.Profile,m.Normal,m.Rare,m.Broken}).ToArray();
            var watcher=new ReplayWatcher(store);
            for(int i=0;i<count*3+4;i++)watcher.ScanAsync().GetAwaiter().GetResult();
            watcher.Stop();
            if(count!=store.State.Matches.Count||balances!=JsonSerializer.Serialize(store.State.Baselines))throw new InvalidDataException("Recount changed match count or baseline");
            var result=new{Status="ready",Before=before,After=store.State.Matches.Select(m=>new{m.Id,m.Normal,m.Rare,m.Broken,m.CounterVersion,m.RecountStatus}),Backup=File.Exists(Path.Combine(store.Folder,"history-before-5.0.json"))};
            File.WriteAllText(report,JsonSerializer.Serialize(result,TrackerStore.Json));return 0;
        }
        catch(Exception e){File.WriteAllText(report,JsonSerializer.Serialize(new{Status="error",Message=e.Message}));return 1;}
    }
    private static int VerifyReplay(string replay, string report)
    {
        string folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!, "integration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string demos = Path.Combine(folder, "demos"); Directory.CreateDirectory(demos);
            string target = Path.Combine(demos, "sample.replay"); System.IO.File.Copy(replay, target);
            System.IO.File.SetLastWriteTimeUtc(target, DateTime.UtcNow.AddMinutes(-2));
            var store = new TrackerStore(Path.Combine(folder, "data"));
            store.State.ReplayFolder = demos; store.State.ImportBeforeStart = true; store.State.Baselines[""] = 100;
            var watcher = new ReplayWatcher(store);
            watcher.ScanAsync().GetAwaiter().GetResult(); watcher.ScanAsync().GetAwaiter().GetResult(); watcher.ScanAsync().GetAwaiter().GetResult();
            if (store.State.Matches.Count != 1) throw new Exception("Automatic import failed: " + string.Join(";", store.State.Log));
            long total = store.Total;
            if (total != 100 + store.State.Matches[0].Total) throw new Exception("Baseline not added correctly");
            var restored = new TrackerStore(store.Folder);
            var again = new ReplayWatcher(restored); again.ScanAsync().GetAwaiter().GetResult();
            if (restored.State.Matches.Count != 1 || restored.Total != total) throw new Exception("Replay duplicated after restart");
            string broken = Path.Combine(demos, "broken.replay"); System.IO.File.WriteAllText(broken, "bad replay");
            System.IO.File.SetLastWriteTimeUtc(broken, DateTime.UtcNow.AddMinutes(-2));
            again.ScanAsync().GetAwaiter().GetResult(); again.ScanAsync().GetAwaiter().GetResult();
            if (restored.State.Matches.Count != 1 || again.ErrorCount != 1) throw new Exception("Invalid replay added as a zero match");
            System.IO.File.WriteAllText(report, $"PASS Automatic detection/import of real replay\nPASS Duplicate rejection\nPASS Restart persistence\nPASS Corrupt replay rejected without a false zero\nMatches: {restored.State.Matches.Count}; total with initial 100: {total}\n" + string.Join("\n", restored.State.Log));
            return 0;
        }
        catch(Exception e) { System.IO.File.WriteAllText(report, "FAIL " + e.Message); return 1; }
        finally { Directory.Delete(folder, true); }
    }
}
