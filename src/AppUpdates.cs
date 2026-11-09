using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChestTracker;

internal sealed record UpdateRelease(Version Version,Uri Download,long Size,string Sha256);
internal sealed record UpdatePlan(string Target,string Candidate,string Sha256,int ParentId,long ParentStartedUtc,bool Restart=false,string[]? RestartArguments=null);

internal sealed class AppUpdates : IDisposable
{
    // Set once before publishing the first release. No user credentials belong in the binary.
    internal static string Repository { get; }=ReadRepository();
    public static bool Configured=>Repository!="";
    public static Version Current=>typeof(AppUpdates).Assembly.GetName().Version!;
    private readonly string folder;
    private readonly HttpClient client;
    private readonly CancellationTokenSource cancellation=new();
    private UpdateRelease? ready;
    private string? candidate;
    private bool busy;
    public bool IsReady=>ready!=null&&candidate!=null;
    public string Status { get; private set; }="Проверка обновлений";
    public event Action? Changed;
    public AppUpdates(string dataDirectory)
    {
        folder=Path.Combine(dataDirectory,"updates");
        client=new HttpClient{Timeout=TimeSpan.FromMinutes(15)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ChestTracker/"+Current);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }
    private static string ReadRepository()
    {
        using var stream=StoragePaths.Asset("update-channel.json");using var json=JsonDocument.Parse(stream);
        string value=json.RootElement.GetProperty("repository").GetString()??"";
        if(value!=""&&!Regex.IsMatch(value,@"\A[A-Za-z0-9-]+/[A-Za-z0-9_.-]+\z"))throw new InvalidDataException("Некорректный канал обновлений");
        return value;
    }
    internal static UpdateRelease? ParseRelease(string json,string repository,Version current)
    {
        using var doc=JsonDocument.Parse(json);var root=doc.RootElement;
        if(root.GetProperty("draft").GetBoolean()||root.GetProperty("prerelease").GetBoolean())return null;
        string tag=root.GetProperty("tag_name").GetString()??"";
        if(!Version.TryParse(tag.TrimStart('v','V'),out var parsed))throw new InvalidDataException("Некорректная версия обновления");
        var version=new Version(parsed.Major,parsed.Minor,Math.Max(0,parsed.Build),Math.Max(0,parsed.Revision));
        if(version<=current)return null;
        var assets=root.GetProperty("assets").EnumerateArray().Where(a=>a.GetProperty("name").GetString()=="ChestTracker.exe").ToArray();
        if(assets.Length!=1)throw new InvalidDataException("В релизе нет единственного ChestTracker.exe");
        var asset=assets[0];long size=asset.GetProperty("size").GetInt64();
        string digest=asset.TryGetProperty("digest",out var value)?value.GetString()??"":"";
        if(!Regex.IsMatch(digest,@"\Asha256:[0-9a-fA-F]{64}\z")||size<1024||size>268435456)throw new InvalidDataException("У обновления нет корректной контрольной суммы или размера");
        var uri=new Uri(asset.GetProperty("browser_download_url").GetString()!);
        string expected="/"+repository+"/releases/download/"+tag+"/ChestTracker.exe";
        if(uri.Scheme!="https"||uri.Host!="github.com"||uri.Port!=443||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!=""||Uri.UnescapeDataString(uri.AbsolutePath)!=expected)
            throw new InvalidDataException("Адрес обновления не принадлежит выбранному репозиторию");
        return new(version,uri,size,digest[7..].ToUpperInvariant());
    }
    private void SetStatus(string text){Status=text;Changed?.Invoke();}
    public async Task CheckAsync()
    {
        if(!Configured||busy||IsReady)return;
        busy=true;string? partial=null;
        try
        {
            SetStatus("Проверяю новую версию…");
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);timeout.CancelAfter(TimeSpan.FromSeconds(25));
            using var response=await client.GetAsync("https://github.com/"+Repository+"/releases/latest/download/update.json",HttpCompletionOption.ResponseHeadersRead,timeout.Token);
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>1048576)throw new InvalidDataException("Слишком большой ответ сервера обновлений");
            var json=await response.Content.ReadAsStringAsync(timeout.Token);
            if(json.Length>1048576)throw new InvalidDataException("Слишком большой ответ сервера обновлений");
            var release=ParseRelease(json,Repository,Current);
            if(release==null){SetStatus("Установлена последняя версия");return;}
            Directory.CreateDirectory(folder);partial=Path.Combine(folder,"download.part");
            SetStatus("Загружаю версию "+release.Version.ToString(3)+"…");
            using var download=await client.GetAsync(release.Download,HttpCompletionOption.ResponseHeadersRead,cancellation.Token);download.EnsureSuccessStatusCode();
            if(download.Content.Headers.ContentLength is long length&&length!=release.Size)throw new InvalidDataException("Размер загрузки не совпадает с релизом");
            await using(var input=await download.Content.ReadAsStreamAsync(cancellation.Token))
            await using(var output=new FileStream(partial,FileMode.Create,FileAccess.Write,FileShare.None,81920,true))
            {
                byte[] buffer=new byte[81920];long received=0;int previous=-1;
                while(true)
                {
                    int n=await input.ReadAsync(buffer,cancellation.Token);if(n==0)break;
                    received+=n;if(received>release.Size)throw new InvalidDataException("Загрузка превысила заявленный размер");
                    await output.WriteAsync(buffer.AsMemory(0,n),cancellation.Token);
                    int percent=(int)(received*100/release.Size);if(percent!=previous){previous=percent;SetStatus($"Загружаю обновление · {percent}%");}
                }
                if(received!=release.Size)throw new InvalidDataException("Загрузка не завершена");
            }
            await Task.Run(()=>VerifyFile(partial,release.Sha256),cancellation.Token);
            candidate=Path.Combine(folder,"ChestTracker-new.exe");File.Move(partial,candidate,true);partial=null;ready=release;
            SetStatus("Версия "+release.Version.ToString(3)+" готова · можно обновить приложение");
        }
        catch(OperationCanceledException){if(!cancellation.IsCancellationRequested)SetStatus("Сервер не ответил. Попробуем позже.");}
        catch(Exception){SetStatus("Не удалось загрузить обновление. Текущая версия продолжает работать.");}
        finally{if(partial!=null)try{File.Delete(partial);}catch{}busy=false;}
    }
    internal static void VerifyFile(string file,string expected)
    {
        using var input=File.OpenRead(file);string actual=Convert.ToHexString(SHA256.HashData(input));
        if(!actual.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Контрольная сумма обновления не совпадает");
        input.Position=0;if(input.ReadByte()!='M'||input.ReadByte()!='Z')throw new InvalidDataException("Обновление не является приложением Windows");
    }
    public void InstallOnExit(bool restart=false)
    {
        if(!IsReady)return;
        string current=Environment.ProcessPath??throw new InvalidOperationException("Не найден EXE приложения");
        string helper=Path.Combine(folder,"apply-update.exe");File.Copy(current,helper,true);
        using var process=Process.GetCurrentProcess();
        var plan=new UpdatePlan(Path.GetFullPath(current),candidate!,ready!.Sha256,process.Id,process.StartTime.ToUniversalTime().Ticks,restart);
        string planFile=Path.Combine(folder,"install.json");File.WriteAllText(planFile,JsonSerializer.Serialize(plan));
        var start=new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
        start.ArgumentList.Add("--apply-update");start.ArgumentList.Add(planFile);
        _ = Process.Start(start)??throw new IOException("Не удалось запустить установку обновления");
    }
    internal static int Apply(string planFile)
    {
        string directory=Path.GetDirectoryName(Path.GetFullPath(planFile))!;
        try
        {
            var plan=JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(planFile))??throw new InvalidDataException("Нет плана обновления");
            if(Path.GetDirectoryName(Path.GetFullPath(plan.Candidate))!=directory||!Path.IsPathFullyQualified(plan.Target)||!plan.Target.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||plan.ParentId<=0)
                throw new InvalidDataException("Некорректный план обновления");
            try
            {
                using var parent=Process.GetProcessById(plan.ParentId);
                if(parent.StartTime.ToUniversalTime().Ticks==plan.ParentStartedUtc&&!parent.WaitForExit(120000))throw new IOException("Приложение ещё не закрыто");
            }
            catch(ArgumentException){} // The parent has already exited.
            ReplaceVerified(plan.Target,plan.Candidate,plan.Sha256,directory);
            File.Delete(planFile);File.WriteAllText(Path.Combine(directory,"result.txt"),"Обновление установлено");
            if(plan.Restart)
            {
                var start=new ProcessStartInfo(plan.Target){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(plan.Target)!};
                foreach(string argument in plan.RestartArguments??[])start.ArgumentList.Add(argument);
                try{_ = Process.Start(start)??throw new IOException("Не удалось запустить приложение");}
                catch(Exception e){File.WriteAllText(Path.Combine(directory,"result.txt"),"Обновление установлено. Перезапустите приложение вручную: "+e.Message);}
            }
            return 0;
        }
        catch(Exception e){try{File.WriteAllText(Path.Combine(directory,"result.txt"),"Обновление не установлено: "+e.Message);}catch{}return 1;}
    }
    internal static void ReplaceVerified(string target,string source,string hash,string cache)
    {
        VerifyFile(source,hash);
        Directory.CreateDirectory(cache);
        string pending=Path.Combine(Path.GetDirectoryName(target)!,"."+Path.GetFileName(target)+".update-"+Guid.NewGuid().ToString("N"));
        try
        {
            File.Copy(source,pending,false);VerifyFile(pending,hash);
            File.Copy(target,Path.Combine(cache,"ChestTracker-previous.exe"),true);
            // The replacement is on the target volume, including when the app runs from another drive.
            File.Replace(pending,target,null);
            File.Delete(source);
        }
        finally{if(File.Exists(pending))File.Delete(pending);}
    }
    public void Dispose(){cancellation.Cancel();client.Dispose();}
}
