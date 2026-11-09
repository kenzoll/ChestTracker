using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChestCounter;

namespace ChestTracker;

public static class SelfTests
{
    public static int Run(string resultPath)
    {
        var report = new List<string>();
        var failures = new List<string>();
        void Test(string name, Action check)
        { try { check(); report.Add("PASS " + name); } catch (Exception e) { report.Add("FAIL " + name + ": " + e.Message); failures.Add(name); } }
        void Require(bool value, string message = "Assertion failed") { if (!value) throw new Exception(message); }
        string root = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(resultPath))!, "test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        ChestRecord C(string blueprint, bool broken = false) => new() { Blueprint = blueprint, Kind = ChestKinds.Classify(blueprint), Destroyed = broken };
        Test("ordinary pirate season chest", () => Require(ChestFilter.Category(C("Tiered_Chest_Athena_C")) == "normal"));
        Test("blue rare chest", () => Require(ChestFilter.Category(C("B_RareChest_Athena_C")) == "rare"));
        Test("rare variant inherits tiered chest name", () => Require(ChestFilter.Category(C("Tiered_Chest_Athena_Rare_C")) == "rare"));
        Test("broken ordinary is included", () => Require(ChestFilter.Category(C("Tiered_Chest_Athena_C", true)) == "normal"));
        Test("shot blue chest is included", () => Require(ChestFilter.Category(C("B_RareChest_Athena_C", true)) == "rare"));
        Test("spirit containers excluded", () => { foreach (var b in new[] { "Tiered_Chest_SpritePoster_C", "BootyChest_C", "RareChest_Spirit_C" }) Require(ChestFilter.Category(C(b)) == ""); });
        Test("ammo food relic and unknown excluded", () => { foreach (var b in new[] { "AmmoBox_C", "FoodBox_C", "RelicChest_C", "BuriedChest_C", "UnknownChest_C", "GoldenChest_C" }) Require(ChestFilter.Category(C(b)) == ""); });
        var record = new MatchRecord { SessionId = "session-one", EpicId = "test-account", PlayerName = "Tester", IsReplayOwner = true, MatchStartUtc = DateTime.UtcNow,
            Chests = [C("Tiered_Chest_Athena_C"), C("B_RareChest_Athena_C", true), C("BootyChest_C")], AmmoBoxesOpened = 2 };
        Test("breakdown and broken not counted twice", () => { var m = ChestFilter.Convert(record); Require(m.Total == 2 && m.Normal == 1 && m.Rare == 1 && m.Broken == 1 && m.Ignored == 3); });
        Test("unidentified recorder rejected", () => { try { ChestFilter.Convert(new MatchRecord()); throw new Exception("Accepted unknown recorder"); } catch (InvalidDataException) { } });
        Test("base total duplicates persistence backup", () =>
        {
            var s = new TrackerStore(Path.Combine(root, "store"));
            s.State.Baselines[""] = 100; s.Save();
            Require(s.Add(ChestFilter.Convert(record))); Require(!s.Add(ChestFilter.Convert(record)));
            Require(s.Total == 102 && s.Current.Count() == 1); s.Save();
            var restored = new TrackerStore(s.Folder); Require(restored.Total == 102 && restored.Current.First().Broken == 1);
            Require(System.IO.File.Exists(s.PathName + ".bak"));
        });
        Test("different accounts do not mix", () =>
        {
            var s = new TrackerStore(Path.Combine(root, "accounts")); s.Add(ChestFilter.Convert(record));
            s.Add(new MatchEntry { Id = "other", Profile = "other", Normal = 40 });
            Require(s.Total == 2); s.State.ActiveProfile = "other"; Require(s.Total == 40);
        });
        Test("empty match stays in history", () => { var s = new TrackerStore(Path.Combine(root, "zero")); s.Add(new MatchEntry { Id = "zero" }); Require(s.Current.Count() == 1 && s.Total == 0); });
        Test("corrupt history is preserved", () =>
        {
            string folder = Path.Combine(root, "bad"); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "tracker-v2.json"); System.IO.File.WriteAllText(path, "bad-json");
            try { new TrackerStore(folder); throw new Exception("Corrupt history accepted"); } catch (JsonException) { }
            Require(System.IO.File.ReadAllText(path) == "bad-json");
        });
        Test("legacy history imported once", () =>
        {
            var s = new TrackerStore(Path.Combine(root, "legacy"));
            string path = Path.Combine(root, "state.json");
            System.IO.File.WriteAllText(path, """{"version":1,"base":80,"current":{"normal":1,"mythic":0},"history":[{"id":"a","time":"2026-11-09T10:00:00","normal":3,"mythic":2}]}""");
            Require(s.ImportLegacy(path) == 2 && s.Total == 86); Require(s.ImportLegacy(path) == 0 && s.Total == 86);
        });
        Test("CSV sums and escaping", () =>
        {
            var s = new TrackerStore(Path.Combine(root, "csv")); s.State.Baselines[""] = 10;
            s.Add(new MatchEntry { Id = "a", Player = "A;B\"C", Normal = 2, Rare = 1, Broken = 1 });
            var csv = s.ExportCsv(s.Current); Require(csv.Contains("A;B\"\"C") && csv.Contains(";2;1;1;3;13;"));
        });
        string MakeReplay(string name, bool live, bool keyMissing)
        {
            string path = Path.Combine(root, name);
            using var f = new BinaryWriter(System.IO.File.Create(path));
            f.Write(0x1CA2E27Fu); f.Write(6u); f.Write(1000u); f.Write(1u); f.Write(1u); f.Write(0);
            f.Write(live ? 1u : 0u); f.Write(0L); f.Write(1u); f.Write(1u); f.Write(16); f.Write(Enumerable.Repeat((byte)(keyMissing ? 0 : 4), 16).ToArray());
            return path;
        }
        Test("recording replay waits for finalization", () => { var h = ReplayProbe.Read(MakeReplay("live.replay", true, true)); Require(h.Valid && h.Live && h.MissingKey); });
        Test("finalized encrypted header accepted", () => { var h = ReplayProbe.Read(MakeReplay("done.replay", false, false)); Require(h.Valid && !h.Live && !h.MissingKey); });
        Test("truncated replay rejected", () => { string path = Path.Combine(root,"invalid.replay"); System.IO.File.WriteAllBytes(path,[1,2,3]); Require(!ReplayProbe.Read(path).Valid); });
        Test("idle header probe performance", () =>
        {
            string path = MakeReplay("perf.replay", false, false); var watch = Stopwatch.StartNew();
            for (int i=0;i<500;i++) Require(ReplayProbe.Read(path).Valid);
            watch.Stop(); report.Add($"METRIC 500 header reads = {watch.ElapsedMilliseconds} ms");
            Require(watch.Elapsed < TimeSpan.FromSeconds(5), "Header probe unexpectedly slow");
        });
        Test("WPF creates all screens", () =>
        {
            var s = new TrackerStore(Path.Combine(root,"ui")); var w = new MainWindow(s, true);
            w.ShowActivated = false; w.Opacity = 0; w.Show(); w.UpdateLayout();
            Require(w.IsLoaded && w.ActualWidth > 0); w.Close();
        });
        Test("Unreal UTC ticks are not mistaken for local time", () =>
        {
            var raw=new DateTime(2026,11,9,10,0,0,DateTimeKind.Unspecified);
            Require(GameClock.ReplayUtc(raw).Kind==DateTimeKind.Utc);
            Require(GameClock.ReplayUtc(raw).Hour==10);
            var east=TimeZoneInfo.CreateCustomTimeZone("test+3",TimeSpan.FromHours(3),"test","test");
            var west=TimeZoneInfo.CreateCustomTimeZone("test-5",TimeSpan.FromHours(-5),"test","test");
            Require(GameClock.InZone(raw,east).Hour==13 && GameClock.InZone(raw,west).Hour==5);
        });
        Test("System zone conversion honors daylight saving", () =>
        {
            var pacific=TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            Require(GameClock.InZone(new DateTime(2026,1,15,12,0,0,DateTimeKind.Utc),pacific).Hour==4);
            Require(GameClock.InZone(new DateTime(2026,7,15,12,0,0,DateTimeKind.Utc),pacific).Hour==5);
        });
        Test("Old timezone repair runs once and preserves backup", () =>
        {
            var s=new TrackerStore(Path.Combine(root,"clock"));
            var old=new DateTime(2026,11,9,10,0,0,DateTimeKind.Utc);
            s.State.Matches.Add(new MatchEntry { Id="clock",TimeUtc=old,Normal=1 }); s.Save();
            var node=System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(s.PathName))!;
            node.AsObject().Remove("TimeEncoding"); System.IO.File.WriteAllText(s.PathName,node.ToJsonString());
            var fixedStore=new TrackerStore(s.Folder);
            var expected=GameClock.RepairV2(old,TimeZoneInfo.Local);
            Require(fixedStore.State.Matches[0].TimeUtc==expected);
            Require(System.IO.File.Exists(s.PathName+".bak"));
            Require(new TrackerStore(s.Folder).State.Matches[0].TimeUtc==expected);
        });
        Test("Portable history migrates without modifying original", () =>
        {
            string portable=Path.Combine(root,"portable");
            var old=new TrackerStore(Path.Combine(portable,"data")); old.State.Baselines[""]=38; old.Save();
            string before=System.IO.File.ReadAllText(old.PathName),target=Path.Combine(root,"local-data");
            StoragePaths.MigratePortableV2(target,portable);
            Require(new TrackerStore(target).Total==38 && System.IO.File.ReadAllText(old.PathName)==before);
            Require(Directory.GetFiles(portable).Length==0);
        });
        Test("Windows zip folder name is recognized during migration", () =>
        {
            string parent=Path.Combine(root,"zip-folder");
            var old=new TrackerStore(Path.Combine(parent,"ChestTracker-v2-Windows","data"));old.State.Baselines[""]=19;old.Save();
            string target=Path.Combine(root,"zip-local-data");StoragePaths.MigratePortableV2(target,parent);
            Require(new TrackerStore(target).Total==19 && System.IO.File.Exists(old.PathName));
        });
        Test("Reward catalog weeks have verified and unknown states", () =>
        {
            Require(RewardCatalog.Weeks.Length==5);
            Require(RewardCatalog.Active(new DateTime(2026,11,9,14,0,0,DateTimeKind.Utc)).Number==2);
            Require(RewardCatalog.Weeks[0].Items.Length==5 && RewardCatalog.Weeks[1].Items.Length==5);
            Require(RewardCatalog.Weeks.Skip(2).All(w=>w.Items.Length==0));
            Require(RewardCatalog.Weeks[1].Items.Any(r=>r.Name=="Diamond and Gold"));
        });
        Test("Embedded map starts with ordinary buried and rare chests", () =>
        {
            using var stream=StoragePaths.Asset("chests.json"); using var doc=JsonDocument.Parse(stream);
            Require(doc.RootElement.GetProperty("season").GetInt32()==43);
            Require(!doc.RootElement.TryGetProperty("sprite_chests",out _));
            var map=new ChestMap(); map.Measure(new Size(900,470));map.Arrange(new Rect(0,0,900,470)); map.EnsureLoaded();
            Require(map.NormalCount==865 && map.RareCount==39 && map.FilteredCount==973);
            map.Filters(false,true); Require(map.FilteredCount==39);
            map.Filters(true,false); Require(map.FilteredCount==865);
            map.Filters(false,false); Require(map.FilteredCount==0);
            Require(map.IsImageLoaded); map.ReleaseImage(); Require(!map.IsImageLoaded);
        });
        Test("Map zoom preserves cursor location and resets", () =>
        {
            var map=new ChestMap();map.Measure(new Size(900,470));map.Arrange(new Rect(0,0,900,470));
            var cursor=new Point(300,150); var world=map.ToWorld(cursor);
            map.ZoomAt(cursor,2); var after=map.ToWorld(cursor);
            Require((world-after).Length<0.000001 && map.Zoom==2);
            map.Fit();Require(map.Zoom==1);
        });
        Test("Every reward and token has decodable embedded artwork", () =>
        {
            foreach(var item in RewardCatalog.Weeks.SelectMany(w=>w.Items).Append(RewardCatalog.Token))
            {
                using var stream=StoragePaths.Asset(item.ImageName);
                var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
                Require(decoder.Frames[0].PixelWidth>=100 && decoder.Frames[0].PixelHeight>=100);
                Require(item.Kind!="Облик");
            }
        });
        Test("Optional map layers start disabled and can be combined", () =>
        {
            var map=new ChestMap();map.EnsureLoaded();Require(map.FilteredCount==973);
            Require(ChestMap.AvailableLayers.Count(l=>map.IsLayerEnabled(l.Id))==3);
            Require(ChestMap.AvailableLayers.Take(3).Select(l=>l.Id).SequenceEqual(new[]{"chests","buried_chests","rare_chests"}));
            map.SetLayer("sprite_chests",true);Require(map.FilteredCount==1018);
            map.SetLayer("ammo_boxes",true);Require(map.FilteredCount==1828);
            map.SetLayer("chests",false);Require(map.FilteredCount==963);
            map.ReleaseImage();
        });
        Test("Reminder queues before replay analysis and persists without duplicates", () =>
        {
            string path=MakeReplay("notify.replay",false,false);
            var store=new TrackerStore(Path.Combine(root,"notification"));
            store.State.StartedUtc=DateTime.UtcNow.AddMinutes(-1);
            store.State.ReplayFolder=root;
            // Isolate exactly one finalized file. First scan must notify before stable-file parsing.
            string demos=Path.Combine(root,"notification-demos");Directory.CreateDirectory(demos);
            System.IO.File.Move(path,Path.Combine(demos,"notify.replay"));store.State.ReplayFolder=demos;
            var watcher=new ReplayWatcher(store);int signals=0;watcher.MatchFinished+=()=>signals++;
            watcher.ScanAsync().GetAwaiter().GetResult();
            Require(signals==1 && store.State.PendingReminder!="" && store.State.Matches.Count==0);
            var loaded=new TrackerStore(store.Folder);
            Require(!ReminderPolicy.Queue(loaded,loaded.State.PendingReminder));
            loaded.State.SpiritReminder=false;Require(!ReminderPolicy.Queue(loaded,"other"));
            loaded.State.SpiritReminder=true;Require(ReminderPolicy.Queue(loaded,"next-match"));
        });
        Test("Match detail UI separates counted events and supports old summary-only matches", () =>
        {
            var owner=new MainWindow(new TrackerStore(Path.Combine(root,"details-ui")),true);
            owner.ShowActivated=false;owner.Opacity=0;owner.Show();
            var match=new MatchEntry{Normal=1,Rare=1,Broken=1,Events=[
                new(){Seconds=90,Category="rare",Counted=true,Destroyed=true},
                new(){Seconds=12,Category="normal",Counted=true},
                new(){Seconds=2,Category="",Counted=false}]};
            var detail=new MatchDetailsWindow(match,owner,false){Opacity=0,ShowActivated=false};detail.Show();detail.UpdateLayout();
            Require(detail.EventRows.Count==2 && detail.EventRows[0].Time=="00:12" && detail.EventRows[1].Action=="Разрушен");detail.Close();
            var legacy=new MatchDetailsWindow(new MatchEntry{Normal=7},owner,false){Opacity=0,ShowActivated=false};legacy.Show();legacy.UpdateLayout();Require(legacy.EventRows.Count==0);legacy.Close();owner.Close();
            Require(MatchDetailsWindow.EventTime(3661)=="1:01:01" && MatchDetailsWindow.EventTime(double.NaN)=="00:00");
        });
        Test("Reward card clicks persist and checkbox square is fully clickable without a tick", () =>
        {
            var store=new TrackerStore(Path.Combine(root,"reward-clicks"));store.State.Animations=false;
            var owner=new MainWindow(store,true){Opacity=0,ShowActivated=false};owner.Show();
            var cards=(System.Windows.Controls.WrapPanel)owner.FindName("RewardCards");
            var card=(System.Windows.Controls.Border)cards.Children[0];
            var check=((System.Windows.Controls.Grid)card.Child).Children.OfType<System.Windows.Controls.CheckBox>().Single();
            card.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});
            Require(check.IsChecked==true&&new TrackerStore(store.Folder).State.FoundRewards.Count==1);
            card.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,System.Windows.Input.MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});
            Require(check.IsChecked==false&&new TrackerStore(store.Folder).State.FoundRewards.Count==0);
            check.ApplyTemplate();var square=(System.Windows.Controls.Border)check.Template.FindName("Box",check);
            Require(square.Background!=null&&square.Child==null);owner.Close();
        });
        string ReleaseJson(string tag="v5.0.0",string? url=null,string? digest=null,bool preview=false)=>JsonSerializer.Serialize(new{
            draft=false,prerelease=preview,tag_name=tag,assets=new[]{new{name="ChestTracker.exe",size=2048,digest=digest??("sha256:"+new string('a',64)),browser_download_url=url??("https://github.com/example/chest-tracker/releases/download/"+tag+"/ChestTracker.exe")}}});
        Test("Updater detects only newer stable versions and normalizes version numbers", () =>
        {
            Require(AppUpdates.ParseRelease(ReleaseJson(),"example/chest-tracker",new Version(4,1,0,0))!.Version==new Version(5,0,0,0));
            Require(AppUpdates.ParseRelease(ReleaseJson("v4.1.0"),"example/chest-tracker",new Version(4,1,0,0))==null);
            Require(AppUpdates.ParseRelease(ReleaseJson(preview:true),"example/chest-tracker",new Version(4,1,0,0))==null);
        });
        Test("Updater rejects missing hashes and download addresses outside the release repository", () =>
        {
            foreach(string payload in new[]{ReleaseJson(digest:""),ReleaseJson(url:"https://elsewhere.example/update.exe"),ReleaseJson(url:"https://github.com/other/repo/releases/download/v5.0.0/ChestTracker.exe")})
            {
                bool rejected=false;try{AppUpdates.ParseRelease(payload,"example/chest-tracker",new Version(4,1,0,0));}catch(InvalidDataException){rejected=true;}Require(rejected);
            }
        });
        Test("Updater replacement keeps history and previous EXE with no adjacent files", () =>
        {
            string install=Path.Combine(root,"update-install"),cache=Path.Combine(root,"update-cache");Directory.CreateDirectory(install);Directory.CreateDirectory(cache);
            string target=Path.Combine(install,"ChestTracker.exe"),source=Path.Combine(cache,"candidate.exe"),history=Path.Combine(install,"history.json");
            System.IO.File.WriteAllText(target,"MZ old version");System.IO.File.WriteAllText(source,"MZ new version");System.IO.File.WriteAllText(history,"preserve history");
            string hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(source)));
            AppUpdates.ReplaceVerified(target,source,hash,cache);
            Require(System.IO.File.ReadAllText(target)=="MZ new version"&&System.IO.File.ReadAllText(Path.Combine(cache,"ChestTracker-previous.exe"))=="MZ old version");
            Require(System.IO.File.ReadAllText(history)=="preserve history"&&Directory.GetFiles(install).Length==2&&!System.IO.File.Exists(source));
        });
        Test("Corrupt update never replaces the installed executable", () =>
        {
            string folder=Path.Combine(root,"update-bad");Directory.CreateDirectory(folder);string target=Path.Combine(folder,"ChestTracker.exe"),source=Path.Combine(folder,"bad.exe");
            System.IO.File.WriteAllText(target,"MZ original");System.IO.File.WriteAllText(source,"MZ damaged");
            bool rejected=false;try{AppUpdates.ReplaceVerified(target,source,new string('0',64),Path.Combine(folder,"cache"));}catch(InvalidDataException){rejected=true;}
            Require(rejected&&System.IO.File.ReadAllText(target)=="MZ original");
        });
        Test("Single executable contains required resources and notices", () =>
        {
            foreach(string name in new[]{"map.png","chests.json","Notices.txt","app.ico"}) { using var stream=StoragePaths.Asset(name);Require(stream.Length>0); }
            Require(StoragePaths.DataDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        });
        System.IO.File.WriteAllText(resultPath, string.Join(Environment.NewLine, report) + $"\n\n{report.Count(l=>l.StartsWith("PASS"))} passed, {failures.Count} failed\n");
        // Only delete the isolated GUID-named fixture folder under the requested report directory.
        Directory.Delete(root, true);
        return failures.Count == 0 ? 0 : 1;
    }
}

internal static class DemoData
{
    public static void Seed(TrackerStore store)
    {
        if (store.State.Matches.Count != 0) return;
        store.State.ActiveProfile = "demo"; store.State.Baselines["demo"] = 1240;
        int[] values = [18,26,12,33,21,19];
        for (int i=0;i<values.Length;i++) store.State.Matches.Add(new MatchEntry { Id = "demo-"+i, Profile="demo", Player="Пример игрока",
            TimeUtc=DateTime.UtcNow.AddMinutes(-25*(values.Length-i)), Normal=values[i], Rare=i%3, Broken=i+2 });
        store.Log("Демонстрационные данные. Не являются результатом реальных матчей.");
    }
}