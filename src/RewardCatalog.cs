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
    public const string Snapshot = "23.11.2026";
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
        new(3,U(15),U(22),"Хацунэ Мику",[
            new("w3-outfit","Solid Gold Miku","Скин","◇"),
            new("w3-corvette","Chevrolet Corvette ZR1 · Golden Llama Crew","Автомобиль и декаль","↗"),
            new("w3-backbling","Miku's Takarabako","Украшение на спину","✦"),
            new("w3-track","PPPP","Музыкальный трек","♪"),
            new("w3-screen","May Fortune Find You","Экран загрузки","▧")]),
        new(4,U(22),U(29),"Касанэ Тето",[
            new("w4-outfit","Solid Gold Kasane Teto","Скин","◇"),
            new("w4-urus","Lamborghini Urus SE · Golden Llama Crew","Автомобиль и декаль","↗"),
            new("w4-backbling","Solid Gold High Tide and Wavecutter","Украшение на спину","✦"),
            new("w4-track","Wellerman","Музыкальный трек","♪"),
            new("w4-screen","Oscar's Wraps","Экран загрузки","▧")]),
        new(5,U(29),U(5,12,19),"Финальная неделя",[])
    ];
    public static readonly RewardItem Token = new("pirate-shop-access","Pirate Shop Access","Жетоны доступа","✦");
    public static string ImageFor(string id) => id switch
    {
        "w1-outfit" => "reward-24988.png", "car-golden-llama-crew" => "reward-car-week1.png",
        "w1-pickaxe" => "reward-25027.png", "w1-track" => "reward-25144.png", "w1-screen" => "reward-25089.png",
        "w2-outfit" => "reward-25181.png", "w2-backbling" => "reward-25182.png", "w2-track" => "reward-25185.png", "w2-screen" => "reward-25183.png",
        "car-gold-cadillac" => "reward-car-week2.png",
        "w3-outfit" => "reward-25271.png", "w3-corvette" => "reward-25273.png",
        "w3-backbling" => "reward-25262.png", "w3-track" => "reward-25274.png", "w3-screen" => "reward-25272.png",
        "w4-outfit" => "reward-25303.png", "w4-urus" => "reward-25306.png",
        "w4-backbling" => "reward-25304.png", "w4-track" => "reward-25302.png", "w4-screen" => "reward-25305.png",
        "pirate-shop-access" => "reward-token.png", _ => throw new InvalidDataException("Нет изображения награды")
    };
    public static RewardWeek Active(DateTime now) => Weeks.LastOrDefault(w => now >= w.StartUtc) ?? Weeks[0];
}
