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
        [RouteArea.FarBeach] = new("Дальний восточный пляж", new(196, 194), 6, "Проверьте хижины за склоном и песок вдоль воды."),
        [RouteArea.NorthCliffs] = new("Скалы у северной дороги", new(174, 85), 6, "Проверьте укрытия вдоль северного склона."),
        [RouteArea.CrabTip] = new("Край песчаного мыса", new(195, 112), 5, "Обойдите край мыса по песку."),
        [RouteArea.RockCove] = new("Отмель у линкора", new(184, 143), 5, "Проверьте берег у северной оконечности островка."),
        [RouteArea.RockRidge] = new("Домики на восточном острове", new(194, 151), 7, "Держитесь тропы за береговым гребнем."),
        [RouteArea.RockPoint] = new("Южный край острова", new(184, 160), 5, "Спуститесь к берегу со стороны холма."),
        [RouteArea.EastFarm] = new("Дома над восточной протокой", new(186, 176), 6, "Проверьте строения на краю дороги."),
        [RouteArea.EastBluff] = new("Дома на восточном обрыве", new(197, 186), 6, "Осмотрите строения и площадки на внешней стороне холма."),
        [RouteArea.SouthDocks] = new("Южные пирсы за бухтой", new(175, 195), 6, "Проверьте пирсы и нижние этажи вдоль воды."),
        [RouteArea.HenryShore] = new("Укрытия на восточном берегу протоки", new(158, 190), 6, "Осмотрите отдельные укрытия у кромки воды."),
        [RouteArea.MillCreek] = new("Домики у мельничного ручья", new(81, 181), 6, "Проверьте домики вдоль ручья с западной стороны."),
        [RouteArea.WestChannel] = new("Дома вдоль западного канала", new(105, 188), 6, "Идите по восточному берегу и загляните в дома у дороги."),
        [RouteArea.WestRidge] = new("Склон над западным парком", new(73, 133), 5, "Проверьте холм и строения севернее парка."),
        [RouteArea.WestBluff] = new("Домики над западным проливом", new(96, 140), 6, "Обыщите строения над обрывом на восточном берегу."),
        [RouteArea.ForestTrail] = new("Хижины у лесной дороги", new(106, 119), 6, "Ищите сундуки в отдельных строениях на краю леса."),
        [RouteArea.ForestLookout] = new("Стоянки над лесной долиной", new(121, 124), 6, "Осмотрите строения на восточном склоне долины."),
        [RouteArea.StoneFrogs] = new("Руины с каменными лягушками", new(117, 151), 7, "Проверьте тропы и открытые площадки вокруг руин."),
        [RouteArea.BridgeHuts] = new("Хижины у южного моста", new(106, 165), 6, "Осмотрите домики и навесы с обеих сторон дороги."),
        [RouteArea.RiverUpper] = new("Домики у верхней речки", new(136, 118), 6, "Проверьте укрытия вдоль речного изгиба."),
        [RouteArea.RiverMid] = new("Укрытия у речной развилки", new(142, 130), 6, "Осмотрите края дороги над речкой."),
        [RouteArea.RiverLookout] = new("Площадки под пустынным склоном", new(158, 130), 5, "Двигайтесь вдоль внешней кромки склона."),
        [RouteArea.DuneCamp] = new("Палатки у восточных дюн", new(162, 136), 6, "Проверьте стоянки на песке над береговой дорогой."),
        [RouteArea.BuggyOutskirts] = new("Дорога южнее лагеря Багги", new(124, 114), 5, "Обыщите отдельные домики вдоль южной дороги."),
        [RouteArea.ResortEast] = new("Домики у северо-западного моста", new(104, 95), 5, "Проверьте строения у моста и край пляжа."),
        [RouteArea.ResortNorth] = new("Северный остров у курорта", new(98, 77), 6, "Пройдите по внешней тропе на маленьком острове."),
        [RouteArea.ResortWest] = new("Западный пляж у курорта", new(78, 98), 5, "Обойдите песок и постройки вдоль воды."),
        [RouteArea.WestCrossing] = new("Пирсы у западной отмели", new(90, 168), 6, "Осмотрите настилы у отмели и домики выше дороги."),
        [RouteArea.LogueOutskirts] = new("Южная окраина Логтауна", new(123, 180), 6, "Проверьте крайние дома и выход к реке."),
        [RouteArea.WestSlope] = new("Домики у западного обрыва", new(73, 154), 6, "Спуститесь к домикам по внешней тропе.")
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
        new(RouteArea.SouthIslets, RouteArea.SouthPier, [new(132, 198), new(130, 201)], "Пересеките протоку к причалу.", true),
        new(RouteArea.NorthHuts, RouteArea.NorthCliffs, [new(172, 82)], ""),
        new(RouteArea.Crab, RouteArea.CrabTip, [new(183, 112), new(190, 111)], ""),
        new(RouteArea.CrabTip, RouteArea.EastBoat, [new(188, 115), new(185, 120)], ""),
        new(RouteArea.RidgeCamp, RouteArea.RockCove, [new(176, 139), new(182, 141)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.RockCove, RouteArea.RockRidge, [new(188, 147)], ""),
        new(RouteArea.RockRidge, RouteArea.RockPoint, [new(190, 157)], ""),
        new(RouteArea.RockPoint, RouteArea.RiverMouth, [new(176, 164), new(170, 164)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.NorthCliffs, RouteArea.EastSteps, [new(179, 87), new(182, 90)], "Следуйте по берегу мимо холма."),
        new(RouteArea.RockPoint, RouteArea.EastShoal, [new(180, 163), new(177, 169)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.EastShoal, RouteArea.EastFarm, [new(181, 173)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.EastFarm, RouteArea.EastBluff, [new(190, 177), new(194, 181)], ""),
        new(RouteArea.EastBluff, RouteArea.FarBeach, [new(197, 190)], ""),
        new(RouteArea.SouthBeach, RouteArea.SouthDocks, [new(177, 201), new(174, 197)], ""),
        new(RouteArea.SouthDocks, RouteArea.HenryShore, [new(170, 195), new(165, 192)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.HenryShore, RouteArea.SouthIslets, [new(150, 191), new(144, 188), new(138, 193)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestPoint, RouteArea.MillCreek, [new(82, 193), new(80, 185)], ""),
        new(RouteArea.MillCreek, RouteArea.WestShoal, [new(78, 178), new(77, 173)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.MillCreek, RouteArea.WestCrossing, [new(85, 177), new(88, 172)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestCrossing, RouteArea.WestShoal, [new(84, 167), new(80, 169)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestCrossing, RouteArea.WestRoad, [new(90, 163), new(91, 159)], ""),
        new(RouteArea.WestSlope, RouteArea.WestShoal, [new(78, 157), new(80, 162), new(78, 167)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestSlope, RouteArea.WestRoad, [new(78, 152), new(86, 153)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestRidge, RouteArea.WestBluff, [new(79, 132), new(85, 134), new(92, 137)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestBluff, RouteArea.WestRoad, [new(96, 146), new(93, 151)], ""),
        new(RouteArea.WestBluff, RouteArea.WestDocks, [new(97, 135), new(96, 130)], ""),
        new(RouteArea.WestDocks, RouteArea.ForestTrail, [new(97, 122), new(101, 121)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.ForestTrail, RouteArea.ForestLookout, [new(111, 117), new(117, 120)], ""),
        new(RouteArea.ForestTrail, RouteArea.NorthShore, [new(109, 113), new(109, 107)], ""),
        new(RouteArea.ForestLookout, RouteArea.BuggyOutskirts, [new(122, 119)], ""),
        new(RouteArea.ForestLookout, RouteArea.RiverMid, [new(130, 124), new(136, 127)], ""),
        new(RouteArea.BuggyOutskirts, RouteArea.SandCamp, [new(117, 110), new(118, 103)], ""),
        new(RouteArea.BuggyOutskirts, RouteArea.RiverUpper, [new(129, 115), new(133, 116)], ""),
        new(RouteArea.RiverUpper, RouteArea.RiverMid, [new(137, 123), new(140, 127)], ""),
        new(RouteArea.RiverUpper, RouteArea.Riverside, [new(142, 121), new(147, 121)], ""),
        new(RouteArea.RiverMid, RouteArea.RiverLookout, [new(148, 130), new(153, 128)], ""),
        new(RouteArea.RiverLookout, RouteArea.DuneCamp, [new(159, 133)], ""),
        new(RouteArea.DuneCamp, RouteArea.RidgeCamp, [new(166, 136)], ""),
        new(RouteArea.Riverside, RouteArea.RiverLookout, [new(154, 124)], ""),
        new(RouteArea.RiverMid, RouteArea.StoneFrogs, [new(135, 134), new(130, 141), new(125, 146)], ""),
        new(RouteArea.StoneFrogs, RouteArea.WestBluff, [new(111, 148), new(104, 146), new(99, 143)], ""),
        new(RouteArea.StoneFrogs, RouteArea.BridgeHuts, [new(111, 154), new(107, 160)], ""),
        new(RouteArea.BridgeHuts, RouteArea.WestChannel, [new(104, 173), new(105, 182)], ""),
        new(RouteArea.BridgeHuts, RouteArea.LogueOutskirts, [new(113, 167), new(114, 174), new(117, 181)], ""),
        new(RouteArea.LogueOutskirts, RouteArea.RingIslet, [new(122, 184)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestChannel, RouteArea.RingIslet, [new(110, 190), new(117, 189)], "Перейдите к соседнему берегу по намеченной линии.", true),
        new(RouteArea.WestChannel, RouteArea.SouthRoad, [new(106, 194)], ""),
        new(RouteArea.ResortWest, RouteArea.ResortEast, [new(84, 99), new(92, 97), new(98, 97)], ""),
        new(RouteArea.ResortNorth, RouteArea.ResortEast, [new(95, 84), new(99, 92)], ""),
        new(RouteArea.ResortEast, RouteArea.NorthShore, [new(107, 99)], "")
    ];

    public static readonly RoutePreset[] Guides =
    [
        new("Северо-восточный берег", [RouteArea.NorthHuts, RouteArea.NorthIslet, RouteArea.EastSteps, RouteArea.Crab, RouteArea.EastBoat]),
        new("Тропы над западным проливом", [RouteArea.PinkIslet, RouteArea.WestDocks, RouteArea.WestBluff, RouteArea.WestRoad, RouteArea.WestCrossing, RouteArea.MillCreek, RouteArea.WestPoint]),
        new("Южная протока", [RouteArea.WestPoint, RouteArea.SouthRoad, RouteArea.RingIslet, RouteArea.SouthIslets, RouteArea.HenryShore, RouteArea.SouthDocks]),
        new("Восточные острова и пляжи", [RouteArea.RockCove, RouteArea.RockRidge, RouteArea.RockPoint, RouteArea.EastShoal, RouteArea.EastFarm, RouteArea.EastBluff, RouteArea.FarBeach, RouteArea.SouthBeach, RouteArea.SouthDocks]),
        new("Речные хижины и дюны", [RouteArea.ForestTrail, RouteArea.ForestLookout, RouteArea.BuggyOutskirts, RouteArea.RiverUpper, RouteArea.RiverMid, RouteArea.RiverLookout, RouteArea.DuneCamp]),
        new("От лесных руин к южному причалу", [RouteArea.StoneFrogs, RouteArea.BridgeHuts, RouteArea.LogueOutskirts, RouteArea.RingIslet, RouteArea.SouthIslets, RouteArea.SouthPier])
    ];

    private static readonly Dictionary<RouteArea, RouteCrossing[]> TrailNeighbors = Trails
        .Concat(Trails.Select(edge => edge.Reverse(keepNote: false))).GroupBy(edge => edge.From)
        .ToDictionary(group => group.Key, group => group.ToArray());

    private static readonly HashSet<RouteArea> MajorStops = [RouteArea.LogueOutskirts];
    private sealed record Candidate(RouteArea[] Stops, int Count, double Cost, int Sector)
    {
        public double Score => Count / (30 + Cost);
        public UInt128 Footprint { get; } = Stops.Aggregate(UInt128.Zero, (mask, stop) => mask | (UInt128.One << (int)stop));
    }

    private Candidate[]? candidates;
    private readonly Queue<Candidate> recentRoutes = new();
    private readonly Queue<Candidate> shownRoutes = new();

    internal static bool IsEligibleArea(RouteArea area) => area != RouteArea.Sandbar && Outskirts.ContainsKey(area);
    internal static bool IsMajorStop(RouteArea area) => MajorStops.Contains(area);

    public RoutePlan Generate(Random? random = null)
    {
        random ??= Random.Shared;
        candidates ??= BuildCandidates();
        var recent = recentRoutes.ToArray();
        var previous = recent.LastOrDefault();
        var unshown = candidates.Where(candidate => shownRoutes.All(shown => Overlap(candidate, shown) < 1)).ToArray();
        var available = unshown.Length == 0 ? candidates : unshown;
        var pool = available.Where(candidate => recent.All(shown => Overlap(candidate, shown) <= 0.58)).ToArray();
        if (pool.Length == 0)
        {
            double leastOverlap = available.Min(candidate => Overlap(candidate, previous));
            pool = available.Where(candidate => Overlap(candidate, previous) <= leastOverlap).ToArray();
        }

        var sectors = pool.GroupBy(candidate => candidate.Sector).ToArray();
        var differentSector = sectors.Where(sector => sector.Key != previous?.Sector).ToArray();
        if (differentSector.Length > 0) sectors = differentSector;
        var sectorPool = sectors[random.Next(sectors.Length)].OrderByDescending(candidate => candidate.Score).ToArray();
        var choices = sectorPool.Where(candidate => candidate.Score >= sectorPool[0].Score * 0.72).Take(24).ToArray();
        var selected = choices[random.Next(choices.Length)];
        recentRoutes.Enqueue(selected);
        if (recentRoutes.Count > 5) recentRoutes.Dequeue();
        shownRoutes.Enqueue(selected);
        if (shownRoutes.Count > 18) shownRoutes.Dequeue();
        return Plan(new(Outskirts[selected.Stops[0]].Name + " → " + Outskirts[selected.Stops[^1]].Name, selected.Stops));
    }

    private Candidate[] BuildCandidates()
    {
        var guides = Guides.Select(guide => new Candidate(guide.Areas, 0, 0, 0)).ToArray();
        var options = new List<Candidate>();
        var edgeCosts = TrailNeighbors.Values.SelectMany(edges => edges)
            .ToDictionary(edge => (edge.From, edge.To), CrossingCost);
        foreach (var start in Outskirts.Keys.Where(IsEligibleArea))
        {
            if (outskirtsPins[start].Length < 3) continue;
            var path = new List<RouteArea> { start };
            Visit(start, outskirtsPins[start].Length, 0, MajorStops.Contains(start) ? 1 : 0);
            void Visit(RouteArea area, int count, double cost, int majorStops)
            {
                if (count >= 40 && path.Count >= 4)
                {
                    double x = path.Average(stop => Outskirts[stop].Center.X);
                    double y = path.Average(stop => Outskirts[stop].Center.Y);
                    int sector = (x < 112 ? 0 : x < 155 ? 1 : 2) + (y < 120 ? 0 : y < 166 ? 3 : 6);
                    var candidate = new Candidate(path.ToArray(), count, cost, sector);
                    if (guides.All(guide => Overlap(candidate, guide) <= 0.80)) options.Add(candidate);
                }
                if (path.Count >= 8) return;
                foreach (var edge in TrailNeighbors[area])
                {
                    if (!IsEligibleArea(edge.To) || path.Contains(edge.To) || outskirtsPins[edge.To].Length < 3) continue;
                    double nextCost = cost + edgeCosts[(area, edge.To)];
                    int nextMajor = majorStops + (MajorStops.Contains(edge.To) ? 1 : 0);
                    if (nextCost > 132 || nextMajor > 1) continue;
                    path.Add(edge.To);
                    Visit(edge.To, count + outskirtsPins[edge.To].Length, nextCost, nextMajor);
                    path.RemoveAt(path.Count - 1);
                }
            }
        }
        return options.Count > 0 ? options.ToArray() : guides
            .Select(guide => guide with { Count = guide.Stops.Sum(stop => outskirtsPins[stop].Length) })
            .Where(guide => guide.Count >= 40).ToArray();
    }

    private static double CrossingCost(RouteCrossing edge)
    {
        var points = edge.Via.Prepend(Outskirts[edge.From].Center).Append(Outskirts[edge.To].Center).ToArray();
        return points.Zip(points.Skip(1)).Sum(pair => (pair.First - pair.Second).Length) + (edge.Water ? 8 : 0);
    }

    private static double Overlap(Candidate candidate, Candidate? other)
    {
        if (other == null) return 0;
        int shared = (int)UInt128.PopCount(candidate.Footprint & other.Footprint);
        return (double)shared / Math.Min(candidate.Stops.Length, other.Stops.Length);
    }
}
