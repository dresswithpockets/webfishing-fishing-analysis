using ConsoleTables;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.ImageExport;
using Plotly.NET.LayoutObjects;
using WebFishingFishWeighting;
using Chart = Plotly.NET.CSharp.Chart;

var lures = Enum.GetValues<Lure>();

await Resources.LoadBaitData();
await Resources.LoadLootTables();

BaitQualityCalculator.CalculateAndPrintQualityProbabilities();
ItemDropCalculator.CalculateAndPrintDropRatesNoReRoll();

await GeneratePlainDropRateCsv(false);
await GeneratePlainDropRateCsv(true);

Console.WriteLine($"Total Weight Freshwater: {Resources.LootTables["lake"].TotalWeight}");
Console.WriteLine($"Total Weight Saltwater: {Resources.LootTables["ocean"].TotalWeight}");

GenerateWormsErrorRate();

Directory.CreateDirectory("Distributions");

var distributions = (await ItemSizeCalculator.CalculateSizeDistributions(1000000)).ToList();
foreach (var (itemName, distribution) in distributions)
    WriteDistributionHistograms(itemName, distribution).Wait(); 

return;

void GenerateWormsErrorRate()
{
    var errorRatesPerLootTable =
        Resources.LootTables.ToDictionary(lt => lt.Key, lt => ItemDropCalculator.CalculateWormsErrorRates(lt.Value));

    Console.WriteLine("Worm Error Rates");
    var table = new ConsoleTable("Loot Table", "Default Attempts", "In Rain Attempts");
    foreach (var (name, values) in errorRatesPerLootTable)
        table.AddRow(name, 1 / values.DefaultChance, 1 / values.InRainChance);
    table.Write();
}

async Task GeneratePlainDropRateCsv(bool inVoid)
{
    var oceanLootTable = Resources.LootTables["ocean"];
    var lakeLootTable = Resources.LootTables["lake"];

    var csvEntries =
        new Dictionary<string, Dictionary<(string, Lure), (double DefaultChance, double InRainChance)>>();

    foreach (var (_, table) in Resources.LootTables)
    foreach (var (item, _) in table.Items)
        csvEntries.Add(item, new Dictionary<(string, Lure), (double DefaultChance, double InRainChance)>());

    csvEntries.Add("Treasure Chest", new Dictionary<(string, Lure), (double DefaultChance, double InRainChance)>());

    foreach (var lure in lures)
    {
        foreach (var (item, defaultChance, inRainChance) in ItemDropCalculator.CalculateDropRates(oceanLootTable, lure, false, inVoid))
        {
            var csvEntry = csvEntries[item.ItemName];
            csvEntry.Add(("ocean", lure), (defaultChance, inRainChance));
        }

        foreach (var (item, defaultChance, inRainChance) in ItemDropCalculator.CalculateDropRates(lakeLootTable, lure, false, inVoid))
        {
            var csvEntry = csvEntries[item.ItemName];
            csvEntry.Add(("lake", lure), (defaultChance, inRainChance));
        }
    }

    var name = "AllDropRates.csv";
    if (inVoid)
        name = "AllDropRates-InVoid.csv";
    await WritePlainDropRateCsv(name, csvEntries);
}

async Task WritePlainDropRateCsv(string fileName, Dictionary<string, Dictionary<(string, Lure), (double DefaultChance, double InRainChance)>> dropTable)
{
    await using var fileStream = new StreamWriter(fileName);

    await fileStream.WriteAsync("Name");
    foreach (var lure in lures)
        await fileStream.WriteAsync($",{lure} (Freshwater),{lure} (Freshwater In-Rain),{lure} (Saltwater),{lure} (Saltwater In-Rain)");
    await fileStream.WriteLineAsync();

    foreach (var (itemName, tableLureMap) in dropTable)
    {
        await fileStream.WriteAsync(itemName);
        foreach (var lure in lures)
        {
            if (tableLureMap.TryGetValue(("lake", lure), out var freshwater))
                await fileStream.WriteAsync($",{freshwater.DefaultChance},{freshwater.InRainChance}");
            else
                await fileStream.WriteAsync($",0.0,0.0");


            if (tableLureMap.TryGetValue(("ocean", lure), out var saltwater))
                await fileStream.WriteAsync($",{saltwater.DefaultChance},{saltwater.InRainChance}");
            else
                await fileStream.WriteAsync($",0.0,0.0");
        }

        await fileStream.WriteLineAsync();
    }
}

async Task WriteDistributionFile(string itemName, List<double> distribution)
{
    var fileName = Path.Join("Distributions", $"{itemName}.csv");
    await using var fileStream = new StreamWriter(fileName);
    foreach (var entry in distribution)
        await fileStream.WriteLineAsync(entry.ToString("F"));
}

async Task WriteDistributionHistograms(string itemName, List<double> itemDistribution)
{
    var min = double.MaxValue;
    var max = 0.0;
    var sum = 0.0;
    foreach (var entry in itemDistribution)
    {
        if (entry > max) max = entry;
        if (entry < min) min = entry;
        sum += entry;
    }

    var avg = sum / itemDistribution.Count;

    var chart = Chart.Histogram<double, double, double>(itemDistribution);
    chart.WithAnnotation(
        Annotation.init<double, double, double, double, double, double, double, double, double, double>(
            X: avg,
            Y: 500,
            Text: $"Average: {avg:F} cm"));

    var fileName = Path.Join("Distributions", $"{itemName}");
    await chart.SavePNGAsync(fileName);
}
