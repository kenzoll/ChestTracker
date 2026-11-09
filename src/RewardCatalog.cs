namespace ChestTracker;

public sealed record RewardItem(string Id, string Name, string Kind, string Symbol)
{
    public string ImageName => RewardCatalog.ImageFor(Id);
}
public sealed record RewardWeek(int Number, DateTime StartUtc, DateTime EndUtc, string Title, RewardItem[] Items)
{
    public string Status(DateTime now) => Items.Length == 0 ? "Состав ещё не опубликован" : now < StartUtc ? "Скоро" : now >= EndUtc ? "Завершена" : "Сейчас доступна";
}
public static class RewardCatalog
{
    public const string Source = "https://fortnite.gg/treasure-hunt";
    public const string Snapshot = "09.11.2026";
    private static DateTime U(int day, int month=11, int hour=14) => new(2026,month,day,hour,0,0,DateTimeKind.Utc);
    public static readonly RewardWeek[] Weeks = [
        new(1,U(1),U(8),"A$AP Rocky",[
            new("w1-outfit","Solid Gold Rugahand","Скин","◇"),
            new("car-golden-llama-crew","G-Wagon · Golden Llama Crew","Автомобиль и декаль","↗"),
            new("w1-pickaxe","Solid Gold Gauntlets","Кирка","✦"),
            new("w1-track","NO TRESPASSING","Музыкальный трек","♪"),
            new("w1-screen","Plunder in Paradise","Экран загрузки","▧")]),
        new(2,U(8),U(15),"Lil Uzi Vert",[
            new("w2-outfit","Solid Gold Lil Uzi Vert","Скин","◇"),
            new("car-gold-cadillac","Gold Cadillac","Автомобиль","↗"),
            new("w2-backbling","Diamond and Gold","Украшение на спину","✦"),
            new("w2-track","London","Музыкальный трек","♪"),
            new("w2-screen","So Kawaii","Экран загрузки","▧")]),
        new(3,U(15),U(22),"Следующая глава охоты",[]),
        new(4,U(22),U(29),"Новые сокровища",[]),
        new(5,U(29),U(5,12,19),"Финальная неделя",[])
    ];
    public static readonly RewardItem Token = new("pirate-shop-access","Pirate Shop Access","Жетоны доступа","✦");
    public static string ImageFor(string id) => id switch
    {
        "w1-outfit" => "reward-24988.png", "car-golden-llama-crew" => "reward-car-week1.png",
        "w1-pickaxe" => "reward-25027.png", "w1-track" => "reward-25144.png", "w1-screen" => "reward-25089.png",
        "w2-outfit" => "reward-25181.png", "w2-backbling" => "reward-25182.png", "w2-track" => "reward-25185.png", "w2-screen" => "reward-25183.png",
        "car-gold-cadillac" => "reward-car-week2.png",
        "pirate-shop-access" => "reward-token.png", _ => throw new InvalidDataException("Нет изображения награды")
    };
    public static RewardWeek Active(DateTime now) => Weeks.LastOrDefault(w => now >= w.StartUtc) ?? Weeks[0];
}
