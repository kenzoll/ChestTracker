using System.Windows;

namespace ChestTracker;

internal enum RouteArea
{
    Resort, Chest, Lagoon, Fort, Lantern, Arlong, Mill, Logue, Cavern, Port, Bay, Rocky, Boat, Battlefield, Skull, Baratie, Henry, NorthIslet
}

internal sealed record RouteDistrict(string Name, Point Center, double Radius, string Search);
internal sealed record RouteCrossing(RouteArea From, RouteArea To, Point[] Via, string Note, bool Water = false)
{
    public RouteCrossing Reverse() => new(To, From, Via.Reverse().ToArray(), Note, Water);
}
internal sealed record RoutePreset(string Name, string Summary, RouteArea[] Areas);
internal sealed record RouteStage(RouteDistrict District, string Instruction, ChestPin[] Pins);
internal sealed record RoutePlan(string Name, string Summary, RouteArea[] Areas, RouteStage[] Stages, RouteCrossing[] Crossings)
{
    public int Count => Stages.Sum(stage => stage.Pins.Length);
}
public sealed record SavedRoute(string Id, string Name, string[] Areas);

internal sealed class ChestRoutes
{
    private static readonly Dictionary<RouteArea, RouteDistrict> Districts = new()
    {
        [RouteArea.Resort] = new("Резиновый курорт", new(100.19, 88.97), 14, "Осмотрите корпуса курорта и западные постройки у склона."),
        [RouteArea.Chest] = new("Гигантский сундук", new(93.02, 109.31), 8, "Начните с бугорков на песчаной косе. Обойдите мегасундук по берегу."),
        [RouteArea.Lagoon] = new("Лагуна добычи", new(132.73, 108.57), 13, "Проверьте здания у берега, затем площадки вокруг лагуны."),
        [RouteArea.Fort] = new("Форт Фортуны", new(161.68, 104.25), 13, "Загляните в постройки вдоль склона и у нижнего входа."),
        [RouteArea.Lantern] = new("Фонарная пристань", new(110.66, 131.83), 13, "Проверьте крыши и этажи, затем загляните в постройки по краю района."),
        [RouteArea.Arlong] = new("Арлонг-парк", new(76.57, 141.34), 13, "Начните с верхних этажей главного здания и спуститесь к дворам."),
        [RouteArea.Mill] = new("Деревня Мельниц", new(92.03, 181.95), 13, "Обыщите дома вдоль берега и постройки вокруг мельниц."),
        [RouteArea.Logue] = new("Логтаун", new(122.28, 174.79), 13, "Идите от крыш к улицам и проверяйте нижние этажи."),
        [RouteArea.Cavern] = new("Проклятая пещера", new(147.53, 144.49), 11, "Начните с верхних уступов, спускайтесь к боковым проходам."),
        [RouteArea.Port] = new("Пиратский порт", new(143.16, 203.73), 12, "Проверьте склады у причала и постройки вдоль берега."),
        [RouteArea.Bay] = new("Оживлённая бухта", new(173.85, 187.63), 12, "Пройдите по берегу и проверьте ближайшие здания."),
        [RouteArea.Rocky] = new("Линкор A$AP Rocky", new(177.44, 153.44), 13, "Осмотрите палубу и нижние уровни корабля."),
        [RouteArea.Boat] = new("Танцевальный катер", new(181.34, 123.96), 14, "Осмотрите пляж и постройки у причала."),
        [RouteArea.Battlefield] = new("Поле битвы великанов", new(117.62, 156.81), 12, "Проверьте руины и площадки вдоль тропы через поле."),
        [RouteArea.Skull] = new("Солёный череп", new(150.36, 154.40), 11, "Ищите сундуки в укрытиях под уступами и у южного выхода из пещеры."),
        [RouteArea.Baratie] = new("Барати", new(176.84, 187.03), 12, "Проверьте ярусы ресторана и пристань."),
        [RouteArea.Henry] = new("Остров Генри", new(149.98, 190.07), 10, "Обойдите маленькие островки по берегу и загляните в укрытия."),
        [RouteArea.NorthIslet] = new("Северо-восточный островок", new(197, 78), 7, "Обойдите пляж по кругу и раскопайте бугорки на песке.")
    };

    private static readonly RouteCrossing[] Crossings =
    [
        new(RouteArea.Resort, RouteArea.Chest, [new(99, 99), new(96, 103)], "Переправа между курортом и песчаной косой.", true),
        new(RouteArea.Chest, RouteArea.Lantern, [new(101, 109), new(103, 120), new(108, 127)], "Короткая переправа с косы, дальше по суше к пристани.", true),
        new(RouteArea.Chest, RouteArea.Lagoon, [new(102, 109), new(114, 108), new(124, 108)], "Переправа с косы, затем на восток вдоль северного берега.", true),
        new(RouteArea.Lagoon, RouteArea.Fort, [new(140, 107), new(150, 107), new(156, 106)], "Держитесь прибрежного прохода между лагуной и фортом."),
        new(RouteArea.Fort, RouteArea.NorthIslet, [new(173, 97), new(180, 86)], "Длинная переправа. Проверьте круг и запас мобильности.", true),
        new(RouteArea.Fort, RouteArea.Boat, [new(161, 110), new(162, 117), new(173, 120)], "Спуск на юг, затем вдоль песчаного полуострова."),
        new(RouteArea.Lagoon, RouteArea.Lantern, [new(126, 113), new(119, 123)], "Держитесь тропы между лагуной и пристанью."),
        new(RouteArea.Lagoon, RouteArea.Cavern, [new(133, 116), new(140, 127), new(144, 137)], "Пройдите с северной стороны по суше вдоль речной долины."),
        new(RouteArea.Boat, RouteArea.Cavern, [new(170, 127), new(156, 133)], "Следуйте по песчаной дороге к северному уступу пещеры."),
        new(RouteArea.Boat, RouteArea.Rocky, [new(178, 130), new(176, 143)], "Переправа к линкору со стороны пляжа.", true),
        new(RouteArea.Arlong, RouteArea.Lantern, [new(87, 143), new(97, 143), new(103, 136)], "Используйте дорогу через узкий пролив."),
        new(RouteArea.Arlong, RouteArea.Mill, [new(78, 151), new(81, 163), new(82, 171), new(90, 175)], "Держитесь западного побережья. У отмели выбирайте короткую переправу.", true),
        new(RouteArea.Lantern, RouteArea.Battlefield, [new(110, 140), new(113, 149)], "Переход по суше вдоль тропы через руины."),
        new(RouteArea.Lantern, RouteArea.Cavern, [new(120, 135), new(132, 135), new(142, 141)], "Идите через центральную часть острова к верхнему уступу."),
        new(RouteArea.Battlefield, RouteArea.Mill, [new(105, 163), new(98, 168), new(97, 178)], "Спуститесь к юго-западному берегу. Есть короткая переправа.", true),
        new(RouteArea.Battlefield, RouteArea.Logue, [new(116, 164), new(119, 170)], "Пройдите по суше между руинами и городом."),
        new(RouteArea.Battlefield, RouteArea.Skull, [new(132, 156), new(141, 154)], "Идите по центральной тропе вдоль южных уступов."),
        new(RouteArea.Cavern, RouteArea.Skull, [new(151, 149)], "Переход между верхними и нижними уровнями пещеры."),
        new(RouteArea.Skull, RouteArea.Logue, [new(142, 164), new(131, 169)], "Переход по южной тропе между городом и подножием пещеры."),
        new(RouteArea.Skull, RouteArea.Rocky, [new(162, 158), new(170, 154)], "Короткая переправа у корабля.", true),
        new(RouteArea.Skull, RouteArea.Bay, [new(156, 163), new(165, 170), new(169, 180)], "Держитесь юго-восточной дороги и перехода через пролив."),
        new(RouteArea.Rocky, RouteArea.Bay, [new(177, 162), new(172, 169), new(169, 179)], "Переправа от корабля, дальше вдоль побережья.", true),
        new(RouteArea.Mill, RouteArea.Logue, [new(97, 173), new(108, 170), new(114, 173)], "Используйте дорогу между деревней и городом."),
        new(RouteArea.Logue, RouteArea.Henry, [new(127, 183), new(138, 190)], "Переправляйтесь между южными островками по коротким отрезкам.", true),
        new(RouteArea.Henry, RouteArea.Port, [new(152, 197), new(147, 201)], "Короткая переправа между островком и портом.", true),
        new(RouteArea.Henry, RouteArea.Baratie, [new(155, 190), new(163, 188), new(170, 188)], "Переправляйтесь через южный пролив.", true),
        new(RouteArea.Port, RouteArea.Bay, [new(153, 202), new(163, 196), new(170, 190)], "Держитесь берега и коротких переправ вдоль бухты.", true),
        new(RouteArea.Bay, RouteArea.Baratie, [new(175, 186)], "Пройдите вдоль пристани у ресторана.")
    ];

    public static readonly RoutePreset[] Guides =
    [
        new("Северная дуга", "Курорт, раскопки и северное побережье до форта.", [RouteArea.Resort, RouteArea.Chest, RouteArea.Lagoon, RouteArea.Fort]),
        new("От парка до Логтауна", "Плотная застройка на западе, затем через руины к городу.", [RouteArea.Arlong, RouteArea.Lantern, RouteArea.Battlefield, RouteArea.Logue]),
        new("Раскопки с выходом в город", "Бугорки на косе, здания у пристани и крыши Логтауна.", [RouteArea.Chest, RouteArea.Lantern, RouteArea.Battlefield, RouteArea.Logue]),
        new("Деревни западного берега", "От Арлонг-парка по побережью к мельницам и улицам Логтауна.", [RouteArea.Arlong, RouteArea.Mill, RouteArea.Logue]),
        new("Через центр к пещере", "Дома у пристани, верхние уровни пещеры и южный выход.", [RouteArea.Lantern, RouteArea.Cavern, RouteArea.Skull, RouteArea.Logue]),
        new("Северо-восточная высадка", "Раскопки на отдельном островке, затем форт и лагуна.", [RouteArea.NorthIslet, RouteArea.Fort, RouteArea.Lagoon]),
        new("Форт, пляж и линкор", "Восточное побережье с переправой на корабль.", [RouteArea.Fort, RouteArea.Boat, RouteArea.Rocky, RouteArea.Bay]),
        new("Вдоль южных причалов", "Из Логтауна через островки к складам пиратского порта.", [RouteArea.Logue, RouteArea.Henry, RouteArea.Port, RouteArea.Bay]),
        new("От Логтауна до Барати", "Город, южные островки и ресторан у пристани.", [RouteArea.Logue, RouteArea.Henry, RouteArea.Baratie, RouteArea.Bay]),
        new("Из бухты в лагуну", "Подъём с юго-востока через пещеру к северному берегу.", [RouteArea.Bay, RouteArea.Skull, RouteArea.Cavern, RouteArea.Lagoon]),
        new("С запада на север", "От парка через пристань и лагуну до форта.", [RouteArea.Arlong, RouteArea.Lantern, RouteArea.Lagoon, RouteArea.Fort]),
        new("Через весь остров", "Длинный пробег от курорта до южных складов.", [RouteArea.Resort, RouteArea.Chest, RouteArea.Lantern, RouteArea.Battlefield, RouteArea.Logue, RouteArea.Henry, RouteArea.Port]),
        new("Курс на восточный пляж", "Из деревни Мельниц через центральные руины к восточному побережью.", [RouteArea.Mill, RouteArea.Battlefield, RouteArea.Skull, RouteArea.Cavern, RouteArea.Boat]),
        new("С юга к форту", "Из пиратского порта по восточной половине острова.", [RouteArea.Port, RouteArea.Bay, RouteArea.Skull, RouteArea.Cavern, RouteArea.Lagoon, RouteArea.Fort])
    ];

    private static readonly Dictionary<RouteArea, RouteCrossing[]> Neighbors = Crossings
        .Concat(Crossings.Select(crossing => crossing.Reverse())).GroupBy(crossing => crossing.From)
        .ToDictionary(group => group.Key, group => group.ToArray());

    private static readonly Dictionary<string, RouteArea> MapLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RUBBER RESORT"] = RouteArea.Resort, ["THE CHEST"] = RouteArea.Chest, ["LOOT LAGOON"] = RouteArea.Lagoon,
        ["FORTUNE'S FORT"] = RouteArea.Fort, ["LANTERN LANDING"] = RouteArea.Lantern, ["ARLONG PARK"] = RouteArea.Arlong,
        ["WINDMILL VILLAGE"] = RouteArea.Mill, ["LOGUETOWN"] = RouteArea.Logue, ["CURSED CAVERN"] = RouteArea.Cavern,
        ["PLUNDER PORT"] = RouteArea.Port, ["BUSY BAY"] = RouteArea.Bay, ["THE $.$. ROCKY"] = RouteArea.Rocky,
        ["BOOGIE BOAT"] = RouteArea.Boat, ["GIANT'S BATTLEFIELD"] = RouteArea.Battlefield, ["THE SALTY SKULL"] = RouteArea.Skull,
        ["BARATIE"] = RouteArea.Baratie, ["HENRY"] = RouteArea.Henry
    };

    public static string Label(string name) => MapLabels.TryGetValue(name, out var area) ? Districts[area].Name : name;
    public static IEnumerable<(RouteArea Area, string Name)> Starts => Districts.Select(pair => (pair.Key, pair.Value.Name));

    private readonly Dictionary<RouteArea, ChestPin[]> pinsByArea;

    public ChestRoutes(IReadOnlyList<ChestPin> pins)
    {
        // Assign each spawn to at most one district, even where the search areas overlap.
        var assigned = pins.Where(IsChest).Select(pin => (Pin: pin, Area: NearestArea(pin.World)))
            .Where(item => item.Area.HasValue).ToLookup(item => item.Area!.Value, item => item.Pin);
        pinsByArea = Districts.Keys.ToDictionary(area => area, area => assigned[area].ToArray());
    }

    private static RouteArea? NearestArea(Point point)
    {
        // The restaurant and the surrounding bay share a map label cluster.
        // Keep Baratie's immediate piers separate from the rest of the bay.
        if ((point - Districts[RouteArea.Baratie].Center).Length <= 3) return RouteArea.Baratie;
        var nearest = Districts.Where(pair => pair.Key != RouteArea.Baratie && (pair.Value.Center - point).Length <= pair.Value.Radius)
            .OrderBy(pair => (pair.Value.Center - point).LengthSquared).Select(pair => (RouteArea?)pair.Key).FirstOrDefault();
        return nearest;
    }

    public static bool IsChest(ChestPin pin) => pin.Layer is "chests" or "buried_chests" or "rare_chests";

    public RoutePlan Plan(RoutePreset preset)
    {
        var crossings = preset.Areas.Zip(preset.Areas.Skip(1)).Select(pair => Neighbors[pair.First].Single(edge => edge.To == pair.Second)).ToArray();
        var stages = preset.Areas.Select((area, index) =>
        {
            var district = Districts[area];
            var enter = index == 0 ? district.Center : crossings[index - 1].Via.Last();
            var exit = index >= crossings.Length ? (Point?)null : crossings[index].Via.First();
            return new RouteStage(district, district.Search, OrderPins(pinsByArea[area], enter, exit));
        }).ToArray();
        return new(preset.Name, preset.Summary, preset.Areas, stages, crossings);
    }

    public RoutePlan? Open(SavedRoute saved)
    {
        var areas = new List<RouteArea>();
        foreach (string value in saved.Areas)
        {
            if (!Enum.TryParse(value, out RouteArea area) || !Districts.ContainsKey(area) || areas.Contains(area)) return null;
            if (areas.Count > 0 && !Neighbors[areas[^1]].Any(edge => edge.To == area)) return null;
            areas.Add(area);
        }
        return areas.Count < 3 ? null : Plan(new(saved.Name, "", areas.ToArray()));
    }

    public RoutePlan Generate(RouteArea start, int goal, bool avoidWater)
    {
        const int maxStages = 9;
        var path = new List<RouteArea> { start };
        var best = path.ToArray();
        int bestCount = 0;
        double bestCost = double.PositiveInfinity;

        void Visit(RouteArea area, int count, double distance)
        {
            // Compare routes that reach the target by travel cost. If the target
            // is out of reach, choose the route with the most spawn locations.
            if (path.Count >= 3)
            {
                int progress = Math.Min(count, goal);
                double cost = distance + Math.Max(0, count - goal) * 0.25;
                if (progress > bestCount || (progress == bestCount && cost < bestCost))
                {
                    best = path.ToArray();
                    bestCount = progress;
                    bestCost = cost;
                }
                if (count >= goal) return;
            }
            if (path.Count == maxStages || distance >= bestCost && bestCount >= goal) return;
            foreach (var edge in Neighbors[area])
            {
                if (path.Contains(edge.To)) continue;
                path.Add(edge.To);
                Visit(edge.To, count + pinsByArea[edge.To].Length, distance + TravelCost(edge, avoidWater));
                path.RemoveAt(path.Count - 1);
            }
        }

        Visit(start, pinsByArea[start].Length, 0);
        string summary = bestCount < goal ? $"На этом маршруте меньше {goal} возможных сундуков." : "";
        return Plan(new("Маршрут на " + goal + " сундуков", summary, best));
    }

    private static double TravelCost(RouteCrossing edge, bool avoidWater)
    {
        var points = edge.Via.Prepend(Districts[edge.From].Center).Append(Districts[edge.To].Center).ToArray();
        double distance = points.Zip(points.Skip(1)).Sum(pair => (pair.First - pair.Second).Length);
        return distance + (edge.Water ? (avoidWater ? 1000 : 12) : 0);
    }

    private static ChestPin[] OrderPins(ChestPin[] pins, Point enter, Point? exit)
    {
        var remaining = pins.ToList();
        var path = new List<ChestPin>();
        var current = enter;
        while (remaining.Count > 0)
        {
            var next = remaining.MinBy(pin => (pin.World - current).Length -
                (exit is Point destination ? (pin.World - destination).Length * 0.28 : 0))!;
            path.Add(next);
            remaining.Remove(next);
            current = next.World;
        }
        return path.ToArray();
    }
}
