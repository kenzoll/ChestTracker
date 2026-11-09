using System.IO;
using System.Diagnostics;
using System.Text.Json;

namespace ChestTracker;

public sealed class ReplayWatcher(TrackerStore store)
{
    public bool Running { get; private set; } = true;
    public bool Busy { get; private set; }
    public string Status { get; private set; } = "Проверяю повторы Fortnite…";
    public string Detail { get; private set; } = "";
    public string Phase { get; private set; } = "waiting";
    public int FileCount { get; private set; }
    public int OldCount => store.State.Files.Values.Count(f => f.Status == "old");
    public int ErrorCount => store.State.Files.Values.Count(f => f.Status == "error");
    public event Action<bool>? Updated;
    public event Action? MatchFinished;
    public event Action? MatchStarted;
    private readonly HashSet<string> liveFiles=[];
    private readonly Dictionary<string, long> seenSize = [];
    private CancellationTokenSource cancellation = new();
    public void Toggle()
    {
        Running = !Running;
        if (!Running) cancellation.Cancel();
        else { cancellation.Dispose(); cancellation = new(); }
        if (!Running) { Status = "Наблюдение на паузе"; Detail = "Нажмите «Продолжить», когда будете готовы."; Phase = "paused"; }
        Updated?.Invoke(false);
    }
    public void ImportOld()
    {
        store.State.ImportBeforeStart = true;
        foreach (var key in store.State.Files.Where(k => k.Value.Status == "old").Select(k => k.Key).ToList()) store.State.Files.Remove(key);
        store.Save();
    }
    public void RetryErrors()
    {
        foreach (var key in store.State.Files.Where(k => k.Value.Status == "error").Select(k => k.Key).ToList()) store.State.Files.Remove(key);
        store.Save();
    }
    public async Task ScanAsync()
    {
        if (!Running || Busy) return;
        Busy = true;
        var token = cancellation.Token;
        bool changed = false;
        try
        {
            var folder = store.State.ReplayFolder;
            if (!Directory.Exists(folder))
            {
                var missing=HistoryRecount.Next(store,Array.Empty<FileInfo>());
                if(missing!=null){HistoryRecount.Failed(store,missing,"Исходный повтор недоступен");changed=true;}
                Phase = "setup"; Status = "Нужно включить запись повторов";
                Detail = "В Fortnite: Настройки → Игра → Повторы → Записывать повторы. Затем сыграйте матч.";
                FileCount = 0;
                return;
            }
            var files = new DirectoryInfo(folder).GetFiles("*.replay").OrderBy(f => f.LastWriteTimeUtc).ToArray();
            FileCount = files.Length;
            bool pending = false;
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                string key = $"{file.Name}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
                if (store.State.Files.ContainsKey(key)) continue;
                var header = ReplayProbe.Read(file.FullName);
                double age = (DateTime.UtcNow - file.LastWriteTimeUtc).TotalSeconds;
                if (header.Valid && (header.Live || header.MissingKey))
                {
                    if(liveFiles.Add(file.Name+file.CreationTimeUtc.Ticks))MatchStarted?.Invoke();
                    pending = true;
                    Phase = "live"; Status = "Матч записывается";
                    Detail = "Играйте как обычно. Сундуки будут подсчитаны, когда Fortnite завершит запись матча.";
                    continue;
                }
                if (!store.State.ImportBeforeStart && file.LastWriteTimeUtc < store.State.StartedUtc.AddSeconds(-10))
                {
                    store.State.Files[key] = new() { Status = "old", Message = "Повтор до начала наблюдения" };
                    changed = true;
                    continue;
                }
                if(header.Valid&&!header.Live&&!header.MissingKey&&file.LastWriteTimeUtc>=store.State.StartedUtc)
                {
                    string reminderKey="finished:"+file.Name+"|"+file.CreationTimeUtc.Ticks;
                    if(ReminderPolicy.Queue(store,reminderKey)){changed=true;MatchFinished?.Invoke();}
                }
                bool stable = seenSize.GetValueOrDefault(file.FullName, -1) == file.Length;
                seenSize[file.FullName] = file.Length;
                if (!stable || age < 5)
                {
                    pending = true; Phase = "finishing"; Status = "Матч завершён · ожидаю файл";
                    Detail = "Жду, пока Fortnite допишет повтор. Это занимает несколько секунд.";
                    continue;
                }
                if (!header.Valid)
                {
                    if (age < 60) { pending = true; continue; }
                    store.State.Files[key] = new() { Status = "error", Message = header.Error };
                    store.Log($"Не прочитан {file.Name}: {header.Error}");
                    changed = true;
                    continue;
                }
                Phase = "analyzing"; Status = "Считаю сундуки за матч";
                Detail = "Читаю события повтора. Окно можно свернуть — игра не прерывается.";
                Updated?.Invoke(false);
                var result = await AnalyzeAsync(file.FullName, token);
                token.ThrowIfCancellationRequested();
                if (result.Status == "waiting") { pending = true; continue; }
                bool added = result.Match != null && result.Status == "ready" && store.Add(result.Match);
                store.Log($"Анализ {file.Name}: {result.ElapsedMs} мс, пик {result.PeakMemoryMb} МБ");

                store.State.Files[key] = new() { Status = result.Status, Message = result.Message };
                if (result.Status == "error") store.Log($"Ошибка {file.Name}: {result.Message}");
                else if (added) { Phase = "saved"; Status = $"Матч сохранён · +{result.Match!.Total} сундуков"; Detail = result.Match.Categories; }
                changed = true;
                Updated?.Invoke(added);
                // Give the game time between CPU-intensive analyses.
                return;
            }
            if(!pending)
            {
                var request=HistoryRecount.Next(store,files);
                if(request!=null)
                {
                    if(request.File==null){HistoryRecount.Failed(store,request,"Исходный повтор недоступен");changed=true;return;}
                    var oldHeader=ReplayProbe.Read(request.File.FullName);
                    if(!oldHeader.Valid){HistoryRecount.Failed(store,request,"Исходный повтор повреждён");changed=true;return;}
                    if(oldHeader.Live||oldHeader.MissingKey||DateTime.UtcNow-request.File.LastWriteTimeUtc<TimeSpan.FromSeconds(5))return;
                    Phase="recounting";Status="Пересчитываю сохранённые матчи";Detail="Проверяю сундуки по новой системе. История и начальная сумма сохраняются.";Updated?.Invoke(false);
                    var repaired=await AnalyzeAsync(request.File.FullName,token);token.ThrowIfCancellationRequested();
                    request.File.Refresh();
                    if(request.Stamp!=$"{request.File.Name}|{request.File.Length}|{request.File.LastWriteTimeUtc.Ticks}")return;
                    if(repaired.Status=="ready"&&repaired.Match!=null)
                    {
                        try{HistoryRecount.Apply(store,request,repaired.Match);}
                        catch(InvalidDataException e){HistoryRecount.Failed(store,request,e.Message);}
                    }
                    else if(repaired.Status=="waiting")return;
                    else HistoryRecount.Failed(store,request,"Повтор не удалось прочитать новой системой");
                    changed=true;return;
                }
            }
            if (!pending)
            {
                Phase = files.Length == 0 ? "setup" : ErrorCount > 0 ? "error" : "waiting";
                Status = files.Length == 0 ? "Пока нет записанных матчей" : ErrorCount > 0 ? "Есть повторы, которые не удалось прочитать" : "Готово · жду следующий матч";
                Detail = files.Length == 0
                    ? "Включите запись повторов в Fortnite и завершите один матч. Приложение подхватит его автоматически."
                    : ErrorCount > 0 ? "Некоторые повторы не удалось прочитать. Остальные матчи продолжают учитываться."
                    : "Наблюдение включено. Открывайте и разбивайте сундуки — итог появится после матча.";
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Phase = "error"; Status = "Не удалось проверить повторы"; Detail = e.Message;
            store.Log(e.Message);
        }
        finally
        {
            if (changed)
            {
                try { store.Save(); }
                catch(Exception e) { Running = false; Phase = "error"; Status = "Не удалось сохранить историю · наблюдение остановлено"; Detail = e.Message; }
            }
            Busy = false;
            Updated?.Invoke(changed);
        }
    }
    private async Task<AnalysisResult> AnalyzeAsync(string path, CancellationToken token)
    {
        string output = Path.Combine(store.Folder, ".analysis-" + Guid.NewGuid().ToString("N") + ".json");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        using var p = new Process { StartInfo = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true } };
        foreach (var arg in new[] { "--analyze", path, output }) p.StartInfo.ArgumentList.Add(arg);
        // Hexadecimal 512 MiB managed heap cap for the disposable parser only.
        p.StartInfo.Environment["DOTNET_GCHeapHardLimit"] = "20000000";
        p.StartInfo.Environment["DOTNET_GCConserveMemory"] = "7";
        try
        {
            p.Start();
            try { p.PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
            await p.WaitForExitAsync(timeout.Token);
            if (!System.IO.File.Exists(output)) return new() { Message = $"Анализатор завершился с кодом {p.ExitCode}. Итог не сохранён." };
            return JsonSerializer.Deserialize<AnalysisResult>(await System.IO.File.ReadAllTextAsync(output, token), TrackerStore.Json) ?? new() { Message = "Пустой ответ анализатора" };
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(true); } catch { }
            token.ThrowIfCancellationRequested();
            return new() { Message = "Анализ занял больше 3 минут. Возможно, версия повтора ещё не поддерживается." };
        }
        finally { try { System.IO.File.Delete(output); } catch { } }
    }
    public void Stop() { cancellation.Cancel(); Running = false; }
}
