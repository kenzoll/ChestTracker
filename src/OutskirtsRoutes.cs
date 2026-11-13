using System.Windows;

namespace ChestTracker;

internal sealed partial class ChestRoutes
{
    private static readonly Dictionary<RouteArea, RouteDistrict> Outskirts = new()
    {
        [RouteArea.NorthHuts] = new("Домики на северном мысе", new(170, 83), 6, "Проверьте домик и палатки у кромки скал."),
        [RouteArea.NorthIslet] = new("Кольцевой пляж", new(197, 78), 6, "Обойдите песок по кругу и раскопайте бугорки."),
        [RouteArea.EastSteps] = new("Лестница над морем", new(183, 96), 5, "Проверьте площадку, нижний этаж и край скалы."),
        [RouteArea.Crab] = new("Деревянный краб", new(176, 114), 6, "Проверьте песок у опор и поднимитесь на площадку."),
        [RouteArea.EastBoat] = new("Навесы у песчаной бухты", new(184, 125), 5, "Проверьте лодку и новые навесы вдоль песка."),
        [RouteArea.RidgeCamp] = new("Домики под рыжим обрывом", new(169, 138), 5, "Обыщите домики у дороги и площадку под скалой."),
        [RouteArea.EastTower] = new("Дома на восточном гребне", new(162, 154), 5, "Проверьте домики над берегом и уступы ниже."),
        [RouteArea.RiverMouth] = new("Устье реки", new(164, 166), 6, "Пройдите мимо построек на изгибе реки к берегу."),
        [RouteArea.SouthCliff] = new("Тропа под скалой", new(151, 165), 5, "Проверьте настилы и укрытия у подножия."),
        [RouteArea.Riverside] = new("Хижины у речного изгиба", new(151, 122), 7, "Держитесь берега и проверьте отдельные навесы."),
        [RouteArea.NorthWatch] = new("Северный наблюдательный мыс", new(146, 94), 5, "Проверьте площадки у построек, затем спуститесь к дороге."),
        [RouteArea.SandCamp] = new("Палатки на северном гребне", new(122, 100), 5, "Проверьте палатки на гребне над берегом и держитесь западнее лагеря Багги."),
        [RouteArea.NorthShore] = new("Скалы над западной бухтой", new(110, 102), 5, "Проверьте отдельные постройки у обрыва."),
        [RouteArea.Sandbar] = new("Песчаная коса", new(92, 108), 7, "Обойдите песчаные отмели и раскопайте бугорки по пути."),
        [RouteArea.PinkIslet] = new("Островок с розовыми деревьями", new(79, 112), 6, "Обыщите берег и склон среди деревьев."),
        [RouteArea.WestDocks] = new("Пирсы в западном проливе", new(94, 126), 7, "Проверьте настилы и береговые укрытия."),
        [RouteArea.WestRoad] = new("Домики у западной дороги", new(92, 155), 6, "Держитесь дороги и загляните в дома у переправы."),
        [RouteArea.WestShoal] = new("Западные отмели", new(76, 168), 7, "Обойдите мелкие островки и проверьте деревянные настилы."),
        [RouteArea.WestPoint] = new("Берег у рощи Нами", new(86, 200), 7, "Проверьте хижины у песка и кромку рощи."),
        [RouteArea.SouthRoad] = new("Домики у южной дороги", new(108, 199), 7, "Проверьте постройки вдоль дороги и ближайшие настилы."),
        [RouteArea.RingIslet] = new("Круглый островок", new(122, 189), 5, "Обойдите островок по берегу и загляните на площадку в центре."),
        [RouteArea.SouthIslets] = new("Островки в южной протоке", new(133, 194), 7, "Проверьте берега островков и деревянные настилы."),
        [RouteArea.SouthPier] = new("Западный причал у порта", new(127, 203), 6, "Соберите сундуки вдоль берега и у отдельного причала."),
        [RouteArea.EastShoal] = new("Песчаные островки на востоке", new(176, 171), 6, "Проверьте короткие пляжи и укрытия у воды."),
        [RouteArea.SouthBeach] = new("Пляж под южным обрывом", new(181, 205), 7, "Спуститесь к песку и обойдите отдельные постройки у воды."),
        [RouteArea.FarBeach] = new("Дальний восточный пляж", new(196, 194), 6, "Проверьте хижины за склоном и песок вдоль воды.")
    };

    // Keep the crossings between landmarks explicit. Straight lines between arbitrary
    // chest clusters can cut through a town, a cliff or an entire bay.
    private static readonly RouteCrossing[] Trails =
    [
        new(RouteArea.NorthHuts, RouteArea.NorthIslet, [new(174, 80), new(180, 83), new(194, 81)], "Переплывите к ближнему краю островка.", true),
        new(RouteArea.NorthIslet, RouteArea.EastSteps, [new(197, 82), new(188, 88), new(186, 93)], "Возвращайтесь к восточному берегу.", true),
        new(RouteArea.NorthHuts, RouteArea.EastSteps, [new(177, 86), new(180, 90)], "Спускайтесь вдоль внешнего берега."),
        new(RouteArea.EastSteps, RouteArea.Crab, [new(186, 100), new(187, 109), new(181, 112)], "Пересеките пролив к песчаному полуострову.", true),
        new(RouteArea.Crab, RouteArea.EastBoat, [new(181, 114), new(184, 116), new(185, 120), new(184, 123)], "Спуститесь с песчаного гребня к навесам."),
        new(RouteArea.Crab, RouteArea.Riverside, [new(167, 115), new(159, 119)], "Идите вдоль воды к речному изгибу."),
        new(RouteArea.EastBoat, RouteArea.RidgeCamp, [new(181, 127), new(177, 127), new(173, 131)], "Обойдите бухту по западному берегу и спуститесь к домикам."),
        new(RouteArea.RidgeCamp, RouteArea.EastTower, [new(166, 144), new(165, 150)], "Оставайтесь на береговой стороне скал."),
        new(RouteArea.EastTower, RouteArea.RiverMouth, [new(165, 158), new(169, 163)], "Спускайтесь по тропе к устью."),
        new(RouteArea.RiverMouth, RouteArea.SouthCliff, [new(159, 166), new(156, 164)], "Держитесь дороги под скалой."),
        new(RouteArea.RiverMouth, RouteArea.EastShoal, [new(169, 165), new(173, 169)], "Переход через мелкий пролив.", true),
        new(RouteArea.EastShoal, RouteArea.FarBeach, [new(183, 175), new(192, 181), new(196, 188)], "Держитесь восточного берега, минуя бухту.", true),
        new(RouteArea.FarBeach, RouteArea.SouthBeach, [new(197, 198), new(189, 203)], "Спускайтесь вдоль берега к южному пляжу."),
        new(RouteArea.Riverside, RouteArea.NorthWatch, [new(150, 113), new(146, 104), new(145, 98)], "Следуйте по внешнему склону к северной дороге."),
        new(RouteArea.NorthWatch, RouteArea.SandCamp, [new(140, 99), new(136, 103), new(135, 107), new(130, 114), new(123, 114), new(118, 108), new(119, 102)], "Обойдите лагерь Багги с юга по внешней тропе."),
        new(RouteArea.SandCamp, RouteArea.NorthShore, [new(117, 101), new(113, 101)], "Оставайтесь на северной дороге."),
        new(RouteArea.NorthShore, RouteArea.Sandbar, [new(103, 106), new(97, 107)], "Короткая переправа к отмели.", true),
        new(RouteArea.Sandbar, RouteArea.PinkIslet, [new(88, 109), new(83, 111)], "Переплывите на маленький западный остров.", true),
        new(RouteArea.PinkIslet, RouteArea.WestDocks, [new(83, 116), new(90, 119)], "Держитесь мелководья к деревянным настилам.", true),
        new(RouteArea.Sandbar, RouteArea.WestDocks, [new(95, 116), new(94, 121)], "Идите по кромке песка к пирсам.", true),
        new(RouteArea.WestDocks, RouteArea.WestRoad, [new(97, 137), new(99, 146), new(95, 153)], "Держитесь восточного берега пролива."),
        new(RouteArea.WestRoad, RouteArea.WestShoal, [new(84, 154), new(83, 161), new(81, 167)], "Выйдите к отмелям через узкий пролив.", true),
        new(RouteArea.WestShoal, RouteArea.WestPoint, [new(77, 176), new(79, 185), new(83, 193), new(83, 198)], "Обходите деревню по внешней стороне острова.", true),
        new(RouteArea.WestPoint, RouteArea.SouthRoad, [new(91, 198), new(97, 195), new(103, 196)], "Пересеките узкий канал к южной дороге.", true),
        new(RouteArea.SouthRoad, RouteArea.RingIslet, [new(113, 195), new(118, 192)], "Переплывите к ближайшей кромке островка.", true),
        new(RouteArea.RingIslet, RouteArea.SouthIslets, [new(126, 189), new(130, 190)], "Переправа между соседними островками.", true),
        new(RouteArea.SouthRoad, RouteArea.SouthPier, [new(114, 200), new(120, 201)], "Держитесь южной дороги до отдельного причала."),
        new(RouteArea.SouthIslets, RouteArea.SouthPier, [new(132, 198), new(130, 201)], "Пересеките протоку к причалу.", true)
    ];

    public static readonly RoutePreset[] Guides =
    [
        new("Хижины и лодка", [RouteArea.EastBoat, RouteArea.RidgeCamp, RouteArea.EastTower, RouteArea.RiverMouth, RouteArea.SouthCliff]),
        new("Северо-восточный берег", [RouteArea.NorthHuts, RouteArea.NorthIslet, RouteArea.EastSteps, RouteArea.Crab, RouteArea.EastBoat]),
        new("Островки у южного причала", [RouteArea.RingIslet, RouteArea.SouthIslets, RouteArea.SouthPier, RouteArea.SouthRoad]),
        new("Раскопки на песчаной косе", [RouteArea.NorthShore, RouteArea.Sandbar, RouteArea.PinkIslet, RouteArea.WestDocks]),
        new("Пирсы западного пролива", [RouteArea.WestDocks, RouteArea.WestRoad, RouteArea.WestShoal, RouteArea.WestPoint]),
        new("Северные палатки", [RouteArea.NorthWatch, RouteArea.SandCamp, RouteArea.NorthShore, RouteArea.Sandbar]),
        new("От краба до рыжего обрыва", [RouteArea.Riverside, RouteArea.Crab, RouteArea.EastBoat, RouteArea.RidgeCamp]),
        new("Восточные отмели", [RouteArea.SouthCliff, RouteArea.RiverMouth, RouteArea.EastShoal, RouteArea.FarBeach, RouteArea.SouthBeach]),
        new("Хижины южного берега", [RouteArea.WestPoint, RouteArea.SouthRoad, RouteArea.RingIslet, RouteArea.SouthIslets]),
        new("По краю пустыни", [RouteArea.EastBoat, RouteArea.Crab, RouteArea.Riverside, RouteArea.NorthWatch, RouteArea.SandCamp])
    ];

    private static readonly Dictionary<RouteArea, RouteCrossing[]> TrailNeighbors = Trails
        .Concat(Trails.Select(edge => edge.Reverse(keepNote: false))).GroupBy(edge => edge.From)
        .ToDictionary(group => group.Key, group => group.ToArray());

    public bool IsOutskirtsPlan(RoutePlan plan) => plan.Areas.All(Outskirts.ContainsKey);

    private readonly Queue<RouteArea[]> recentRoutes = new();

    public RoutePlan Generate(Random? random = null)
    {
        random ??= Random.Shared;
        var ready = Guides.Select(guide => Signature(guide.Areas)).ToHashSet();
        var recent = recentRoutes.Select(Signature).ToHashSet();
        var options = new List<(RouteArea[] Path, double Score)>();
        foreach (var start in Outskirts.Keys)
        {
            var path = new List<RouteArea> { start };
            Visit(start, outskirtsPins[start].Length, 0, path);
        }

        // Pick a coast first, so a dense beach cannot drown out every other start.
        double best = options.Max(option => option.Score);
        var starts = options.Where(option => option.Score >= best * 0.42).GroupBy(option => option.Path[0]).ToArray();
        var pool = starts[random.Next(starts.Length)].OrderByDescending(option => option.Score).Take(9).ToArray();
        var selected = pool[random.Next(pool.Length)].Path;
        if (recentRoutes.Count >= 12) recentRoutes.Dequeue();
        recentRoutes.Enqueue(selected);
        return Plan(new(Outskirts[selected[0]].Name + " → " + Outskirts[selected[^1]].Name, selected));

        void Visit(RouteArea area, int count, double distance, List<RouteArea> path)
        {
            if (path.Count >= 3 && count >= 17 && distance <= 103)
            {
                var signature = Signature(path);
                if (!ready.Contains(signature) && !recent.Contains(signature))
                {
                    int stopsWithLoot = path.Count(stop => outskirtsPins[stop].Length >= 3);
                    if (stopsWithLoot == path.Count)
                        options.Add((path.ToArray(), count / (28 + distance) * (1 + 0.07 * stopsWithLoot)));
                }
            }
            if (path.Count == 5 || distance >= 103) return;
            foreach (var edge in TrailNeighbors[area])
            {
                if (path.Contains(edge.To)) continue;
                path.Add(edge.To);
                var points = edge.Via.Prepend(Outskirts[edge.From].Center).Append(Outskirts[edge.To].Center).ToArray();
                double length = points.Zip(points.Skip(1)).Sum(pair => (pair.First - pair.Second).Length);
                Visit(edge.To, count + outskirtsPins[edge.To].Length, distance + length + (edge.Water ? 9 : 0), path);
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    private static string Signature(IEnumerable<RouteArea> areas)
    {
        string direct = string.Join(',', areas);
        string reverse = string.Join(',', areas.Reverse());
        return string.CompareOrdinal(direct, reverse) < 0 ? direct : reverse;
    }
}
