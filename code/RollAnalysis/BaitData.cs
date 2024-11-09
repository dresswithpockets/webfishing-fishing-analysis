using System.Text.Json.Serialization;
using ConsoleTables;

namespace RollAnalysis;

public record BaitData(
    [property: JsonPropertyName("catch")] double CatchRate,
    [property: JsonPropertyName("max_tier")] int MaxTier,
    [property: JsonPropertyName("quality")] List<double> QualityBarriers);

public static class BaitQualityCalculator
{
    public const double FishSpawnQualityMultiplier = 1.5;
    
    public static void CalculateAndPrintQualityProbabilities(bool inFishSpawn)
    {
        Console.WriteLine($"Quality Rates Per Bait Type (In Fish Spawn: {inFishSpawn})");
        var qualityProbabilities = CalculateQualityProbabilities(inFishSpawn);
        var table = new ConsoleTable("Bait", "Normal", "Shining", "Glistening", "Opulent", "Radiant", "Alpha");
        foreach (var (key, value) in qualityProbabilities.OrderBy(p => p.Value.Count))
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            var row = new object[] { key, "", "", "", "", "", "" };
            for (var idx = 0; idx < value.Count; idx += 1)
                row[idx + 1] = value[idx].ToString("P");

            table.AddRow(row);
        }

        table.Write();
    }
    
    public static Dictionary<string, List<double>> CalculateQualityProbabilities(bool inFishSpawn)
    {
        var fishSpawnMultiplier = inFishSpawn ? FishSpawnQualityMultiplier : 1.0;
        var baitMap = new Dictionary<string, List<double>>();
        foreach (var bait in Resources.Baits)
        {
            var probabilities = new List<double>(bait.Value.QualityBarriers);
            for (var currentIdx = 0; currentIdx < probabilities.Count; currentIdx += 1)
            {
                var chance = Math.Clamp(probabilities[currentIdx] * fishSpawnMultiplier, 0.0, 1.0);
                for (var nextIdx = currentIdx + 1; nextIdx < probabilities.Count; nextIdx += 1)
                {
                    chance *= 1.0 - Math.Clamp(probabilities[nextIdx] * fishSpawnMultiplier, 0.0, 1.0);
                }

                probabilities[currentIdx] = chance;
            }

            baitMap[bait.Key] = probabilities;
        }
        return baitMap;
    }
}