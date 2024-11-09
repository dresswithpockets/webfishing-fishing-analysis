namespace RollAnalysis;

public static class ItemSizeCalculator
{
	public static async Task<Dictionary<string, List<double>>> CalculateSizeDistributions(int n)
	{
		var items = Resources.LootTables.SelectMany(lt => lt.Value.Items.Values);
		var results = await Task.WhenAll(items.Select(i => CalculateSizeDistribution(i, n)));
		return results.ToDictionary(r => r.Item.ItemName, r => r.Distribution);
	}

	public static async Task<(Item Item, List<double> Distribution)> CalculateSizeDistribution(Item item, int n)
	{
		return await Task.Run(() =>
		{
			var sizes = new List<double>();
			for (var i = 0; i < n; i++)
				sizes.Add(RollItemSize(item));
			return (item, sizes);
		});
	}

    public static double RollItemSize(Item item)
    {
	    var mean = item.AverageSize;
	    var deviation = mean * 0.55;
	    mean *= 1.25;

	    var roll = Stepify(RandNormal(mean, deviation), 0.01);
	    return Math.Max(Math.Abs(roll), 0.01);
    }

    public static readonly Pcg32Random Pcg = new();

    public static double Stepify(double value, double step)
    {
	    if (step != 0)
		    value = Math.Floor(value / step + 0.5) * step;
	    return value;
    }

    public static ulong Rand() => Pcg.Next();

    public static double RandDouble()
    {
	    return (((Rand() << 32) | Rand()) & 0x1FFFFFFFFFFFFFU) / (double)0x1FFFFFFFFFFFFFU;
    }

    public static double RandNormal(double mean, double deviation) =>
	    mean + deviation * (Math.Cos(Math.Tau * RandDouble()) * Math.Sqrt(-2.0 * Math.Log(RandDouble())));
}

public class Pcg32Random
{
	private ulong _state = 12047754176567800795UL;
	private const ulong Increment = 1442695040888963407UL;

	public uint Next()
	{
		var oldState = _state;
		_state = unchecked(oldState * 6364136223846793005UL + Increment);
		var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
		var rot = (int)(oldState >> 59);
		var result = (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
		return result;
	}
}