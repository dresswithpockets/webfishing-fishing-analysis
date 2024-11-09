namespace RollAnalysis;

public delegate Item RollPicker(IEnumerable<Item> rolls);

public static class RollPickers
{
    public static Item PickNormal(IEnumerable<Item> rolls) => rolls.Last();

    public static Item PickSmallest(IEnumerable<Item> rolls) =>
        rolls.MinBy(r => r.AverageSize) ?? throw new InvalidOperationException();

    public static Item PickLargest(IEnumerable<Item> rolls) =>
        rolls.MaxBy(r => r.AverageSize) ?? throw new InvalidOperationException();

    public static Item PickHighestTier(IEnumerable<Item> rolls) =>
        rolls.MaxBy(r => r.Tier) ?? throw new InvalidOperationException();

    public static Item PickRarest(IEnumerable<Item> rolls)
    {
        var enumerable = rolls as Item[] ?? rolls.ToArray();
        var chosen = enumerable[0];
        foreach (var roll in enumerable)
        {
            if (roll.Rare)
                chosen = roll;
        }

        return chosen;
    }
}
