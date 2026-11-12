using System.Windows;

namespace ChestTracker;

internal sealed record RouteStop(string Name, Point Location, string Instruction, double Radius = 0);
internal sealed record ChestRoute(
    string Name, string Summary, RouteStop[] Stops, string? Source = null, string? Layer = null, Point[][]? Via = null);
internal sealed record RoutePlan(ChestRoute Route, IReadOnlyList<ChestPin>[] Pins)
{
    public int Count => Pins.Sum(group => group.Count);
}

internal static class ChestRoutes
{
    private const string ClickGuide = "https://www.theclick.gg/fortnite-pirate-shop-token-farming-routes/";
    private const string TokenGuide = "https://www.fortnitetoken.org/pirate-shop-token/chest-routes/";

    public static readonly ChestRoute[] Guides =
    [
        new("Через центр острова", "Длинный маршрут через четыре локации. Последнюю остановку выбирайте по кругу.",
        [
            new("Lantern Landing", new(110.66, 131.83), "Начните с домов у высадки. Затем двигайтесь на запад к парку.", 8),
            new("Arlong Park", new(76.57, 141.34), "Осмотрите главное здание сверху вниз и соседние дворы. Выходите на юго-восток.", 8),
            new("Loguetown", new(122.28, 174.79), "Проверьте крыши и улицы. Переходите к пещере только при удобном круге.", 8),
            new("Cursed Cavern", new(147.53, 144.49), "Подойдите с юга и осмотрите уступы и нижние уровни.", 8)
        ], "https://fortniteshoptoken.me/guides/best-chest-locations/", Via:
        [
            [new(103, 135), new(97, 143), new(86, 142)],
            [new(79, 152), new(89, 158), new(101, 163), new(112, 167)],
            [new(132, 170), new(145, 160), new(151, 151)]
        ]),
        new("От парка к фонарям", "Две локации с плотной застройкой на западе острова.",
        [
            new("Arlong Park", new(76.57, 141.34), "Высадитесь на крыше, спуститесь по этажам и закончите во дворе.", 8),
            new("Lantern Landing", new(110.66, 131.83), "Идите на северо-восток. У переправы держитесь суши и проверяйте дома по пути.", 8)
        ], ClickGuide, Via: [[new(86, 142), new(97, 143), new(103, 135)]]),
        new("Раскопки у The Chest", "Обход песчаной косы вокруг гигантского сундука. В подсчёте только закопанные точки.",
        [
            new("Восточный берег", new(96, 109), "Начните с восточного края пляжа. Ищите бугорки на песке.", 5.5),
            new("Южная коса", new(91, 111), "Обойдите гигантский сундук с юга, раскапывая точки по ходу.", 5.5),
            new("Западная сторона", new(91, 104), "Двигайтесь на север вдоль песка. Оставьте время на переправу обратно.", 6)
        ], "https://fortniteshoptoken.wiki/chest-routes/the-chest/", "buried_chests"),
        new("Островки к северу от порта", "Короткий маршрут от Loguetown к мелким островам. По пути есть переправы.",
        [
            new("Южный край Loguetown", new(124, 181), "Возьмите оружие в городе и спуститесь к кромке воды.", 5),
            new("Первый островок", new(140, 179), "Переберитесь на восток к ближайшему островку и осмотрите берег.", 5),
            new("Южная отмель", new(141, 192), "Двигайтесь на юг по островкам. Если шторм близко, выходите к порту.", 5.5)
        ], ClickGuide),
        new("Северо-восточный остров", "Высадка на отдельном острове с выходом к Fortune’s Fort.",
        [
            new("Северо-восточный пляж", new(197, 78), "Начните с песка. Обойдите остров по берегу и выкопайте заметные сундуки.", 5.5),
            new("Берег Fortune’s Fort", new(170, 89), "Возвращайтесь на юго-запад к материку. Продолжайте у укрытий на берегу.", 7)
        ], TokenGuide),
        new("Спуск в Cursed Cavern", "Короткий обход пещеры сверху вниз. Перепады высот смотрите в игре.",
        [
            new("Северный уступ", new(146, 140), "Начните на кромке пещеры и подберите сундуки снаружи.", 5),
            new("Площадка в пещере", new(149, 146), "Спуститесь на следующий уступ. Проверьте боковые проходы.", 5),
            new("Южный выход", new(152, 152), "Загляните на нижний уровень и выходите к The Salty Skull.", 5)
        ], TokenGuide)
    ];

    public static bool IsChest(ChestPin pin) => pin.Layer is "chests" or "buried_chests" or "rare_chests";

    public static RoutePlan PlanGuide(ChestRoute guide, IReadOnlyList<ChestPin> pins)
    {
        var remaining = pins.Where(pin => IsChest(pin) && (guide.Layer == null || guide.Layer == pin.Layer)).ToList();
        var groups = new List<IReadOnlyList<ChestPin>>();
        foreach (var stop in guide.Stops)
        {
            var local = remaining.Where(pin => (pin.World - stop.Location).Length <= stop.Radius).ToArray();
            groups.Add(local);
            remaining.RemoveAll(pin => local.Contains(pin));
        }
        return new(guide, groups.ToArray());
    }

    public static RoutePlan Generate(MapPlace start, int limit, string? layer, IReadOnlyList<ChestPin> pins)
    {
        // Search one neighborhood and prefer short connections between nearby chest spots.
        var candidates = pins.Where(pin => IsChest(pin) && (layer == null || pin.Layer == layer)
            && (pin.World - start.Position).Length <= 19).ToList();
        var best = new List<ChestPin>();
        double bestLength = double.PositiveInfinity;
        foreach (var first in candidates.OrderBy(pin => (pin.World - start.Position).LengthSquared).Take(14))
        {
            var path = new List<ChestPin> { first };
            var remaining = candidates.Where(pin => pin.Id != first.Id).ToList();
            while (path.Count < limit && remaining.Count > 0)
            {
                var origin = path[^1].World;
                var next = remaining.MinBy(pin => (pin.World - origin).LengthSquared)!;
                if ((next.World - origin).Length > 7.5) break;
                path.Add(next);
                remaining.Remove(next);
            }
            Shorten(path);
            double length = (path[0].World - start.Position).Length;
            for (int i = 1; i < path.Count; i++) length += (path[i].World - path[i - 1].World).Length;
            if (path.Count < best.Count || (path.Count == best.Count && length >= bestLength)) continue;
            best = path;
            bestLength = length;
        }
        var stops = best.Select((pin, i) => new RouteStop(
            LayerName(pin.Layer), pin.World, i == 0 ? $"Первая точка рядом с {start.Name}."
            : pin.Layer == "buried_chests" ? "Ищите бугорок и раскопайте его." : "Проверьте ближайшее укрытие и соседние этажи.")).ToArray();
        string summary = best.Count switch
        {
            0 => "Рядом с этой высадкой нет отмеченных точек выбранного типа. Выберите другой тип или место.",
            _ when best.Count < limit => $"Рядом с {start.Name} нашлось {best.Count} из {limit} запрошенных точек в пределах коротких переходов.",
            _ => $"Порядок сбора от {start.Name} с короткими переходами между точками."
        };
        return new(new("Маршрут от " + start.Name, summary, stops), best.Select(pin => (IReadOnlyList<ChestPin>)new[] { pin }).ToArray());
    }

    private static void Shorten(List<ChestPin> path)
    {
        // Keep the landing point and reverse a section only if both new links stay short.
        for (int i = 1; i < path.Count - 2; i++)
        {
            for (int j = i + 1; j < path.Count - 1; j++)
            {
                double first = (path[i - 1].World - path[j].World).Length;
                double second = (path[i].World - path[j + 1].World).Length;
                double previous = (path[i - 1].World - path[i].World).Length + (path[j].World - path[j + 1].World).Length;
                if (first <= 7.5 && second <= 7.5 && first + second < previous) path.Reverse(i, j - i + 1);
            }
        }
    }

    public static string LayerName(string layer) => layer switch
    {
        "buried_chests" => "Закопанный сундук",
        "rare_chests" => "Редкий сундук",
        _ => "Обычный сундук"
    };
}
