namespace ChestTracker;

public static class StoragePaths
{
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChestTrackerDesktop");
    public static void MigratePortableV2(string destination, string executableDirectory)
    {
        string target = Path.Combine(destination, "tracker-v2.json");
        if (File.Exists(target)) return;
        var candidates = new[] {
            Path.Combine(executableDirectory,"data","tracker-v2.json"),
            Path.Combine(executableDirectory,"ChestTracker-v2","data","tracker-v2.json"),
            Path.Combine(executableDirectory,"ChestTracker-v2-Windows","data","tracker-v2.json"),
            Path.Combine(executableDirectory,"..","ChestTracker-v2","data","tracker-v2.json"),
            Path.Combine(executableDirectory,"..","ChestTracker-v2-Windows","data","tracker-v2.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"ChestTracker-v2-Windows","data","tracker-v2.json"),
            Path.Combine(executableDirectory,"..","data","tracker-v2.json")
        };
        foreach(string file in candidates.Select(Path.GetFullPath).Distinct())
        {
            if (!File.Exists(file) || file == Path.GetFullPath(target)) continue;
            Directory.CreateDirectory(destination);
            // Copy only. The portable installation remains an untouched fallback.
            File.Copy(file,target,false);
            return;
        }
    }
    public static Stream Asset(string name) => typeof(StoragePaths).Assembly.GetManifestResourceStream("ChestTracker.Assets." + name)
        ?? throw new FileNotFoundException("Не найден встроенный ресурс: " + name);
}
