using System.Text.Json;

namespace WebFishingFishWeighting;

public class Resources
{
    public static readonly Dictionary<string, LootTable> LootTables = new();
    public static readonly Item TreasureChest = new("Treasure Chest", "treasure", 0, 0, 0, "", 0, 60, 0, 0, false);
    public static Item VoidFish = null!;
    public static Dictionary<string, BaitData> Baits = null!;

    private static readonly string[] CreaturePaths = [
        "Resources/Creatures_Fish_Alien",
        "Resources/Creatures_Fish_Freshwater",
        "Resources/Creatures_Fish_Ocean",
        "Resources/Creatures_Fish_RainSpecial",
        "Resources/Creatures_Fish_Void",
        "Resources/Creatures_WaterTrash",
    ];

    public static async Task LoadBaitData()
    {
        const string filePath = "Resources/BaitData.json";
        var baitDataJson = await File.ReadAllTextAsync(filePath);
        Baits = JsonSerializer.Deserialize<Dictionary<string, BaitData>>(baitDataJson)!;
    }

    public static async Task LoadLootTables()
    {
        LootTables.Clear();

        var dirInfos = CreaturePaths.Select(p => new DirectoryInfo(p)).SelectMany(di => di.EnumerateFiles());
        var fileStreams = dirInfos.Select(f => new StreamReader(f.OpenRead())).ToList();
        var lootWeightTasks = fileStreams.Select(Item.FromResource);
        var items = await Task.WhenAll(lootWeightTasks);

        foreach (var fileStream in fileStreams)
            fileStream.Dispose();

        foreach (var item in items)
        {
            if (!LootTables.TryGetValue(item.LootTable, out var lootTable))
            {
                lootTable = new LootTable();
                LootTables[item.LootTable] = lootTable;
            }

            lootTable.Add(item);
        }

        foreach (var (_, table) in LootTables)
            table.Recalculate();

        VoidFish = LootTables["void"].Items.Values.First();
    }
}

public class LootTable
{
    public readonly Dictionary<string, Item> Items = new();
    public readonly Dictionary<string, double> Chances = new();
    public double TotalWeight;

    public void Add(Item item)
    {
        Items.Add(item.ItemName, item);
        TotalWeight += item.LootWeight;
    }

    public void Recalculate()
    {
        foreach (var (name, item) in Items)
            Chances[name] = item.LootWeight / TotalWeight;
    }
}
