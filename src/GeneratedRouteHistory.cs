namespace ChestTracker;

internal sealed class GeneratedRouteHistory
{
    private const int Limit = 50;
    private readonly List<RoutePlan> plans = [];
    private int index = -1;

    public RoutePlan? Current => index >= 0 ? plans[index] : null;
    public bool HasPrevious => index > 0;
    public bool HasNext => index >= 0 && index < plans.Count - 1;

    public void Add(RoutePlan plan)
    {
        int next = index + 1;
        plans.RemoveRange(next, plans.Count - next);
        plans.Add(plan);
        if (plans.Count > Limit) plans.RemoveAt(0);
        index = plans.Count - 1;
    }

    public void Move(int offset)
    {
        if (plans.Count > 0) index = Math.Clamp(index + offset, 0, plans.Count - 1);
    }
}
