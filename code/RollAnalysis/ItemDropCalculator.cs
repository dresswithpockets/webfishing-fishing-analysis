using ConsoleTables;

namespace RollAnalysis;

public class ItemDropCalculator
{
	// outside of the rain there is a 5% chance that the fish_type will be water_trash instead of the fish_type
	//     of the zone the player is fishing in.
	//     so, outside of the rain, all non-water_trash items will have their chance multiplied by (1 - 0.05)
	//
	// inside the rain there is an 8% chance that the fish_type will be rain, and a 5% * (100% - 8%) chance that
	//     the fish_type will be water_trash.
	//     so, inside the range, all non-water_trash items will have their chance multiplied by
	//	   (1 - 0.05 * (1 - 0.08))
	//
	// regardless of anything before, there is a 2% chance that the caught item is a treasure_chest. So, all chances
	//     will be multiplied by (1 - 0.02) to factor in that chance.
	private const double VoidAltTypeChance = 0.025;
	private const double WaterTrashChance = 0.05;
	private const double RainChance = 0.08;
	private const double TreasureChestChance = 0.02;
	private const double AttractiveTreasureMultiplier = 2.0;
	private const double AttractiveWaterTrashChance = WaterTrashChance * AttractiveTreasureMultiplier;
	private const double AttractiveTreasureChestChance = TreasureChestChance * AttractiveTreasureMultiplier;
	
    public static void CalculateAndPrintDropRatesNoReRoll()
    {
	    Console.WriteLine("Ocean Drop Rates Per Item, With Bare Hook");
	    var table = new ConsoleTable("Item", "Default", "In Rain");
	    var oceanRows = CalculateSingleDropRatesForTable(Resources.LootTables["ocean"], false);
	    foreach (var row in oceanRows)
		    table.AddRow(row.Name, row.DefaultChance.ToString("P"), row.InRainChance.ToString("P"));
	    table.AddRow("TOTAL", oceanRows.Sum(r => r.DefaultChance).ToString("P"),
		    oceanRows.Sum(r => r.InRainChance).ToString("P"));

	    table.Write();
	    
	    Console.WriteLine("Ocean Drop Rates Per Item, With Bare Hook");
	    table = new ConsoleTable("Item", "Default", "In Rain");
	    var lakeRows = CalculateSingleDropRatesForTable(Resources.LootTables["lake"], false);
	    foreach (var row in lakeRows)
		    table.AddRow(row.Name, row.DefaultChance.ToString("P"), row.InRainChance.ToString("P"));
	    table.AddRow("TOTAL", lakeRows.Sum(r => r.DefaultChance).ToString("P"),
		    lakeRows.Sum(r => r.InRainChance).ToString("P"));
	    table.Write();
    }

    public static List<(string Name, int Tier, double DefaultChance, double InRainChance)> CalculateSingleDropRatesForTable(LootTable lootTable, bool disregardTreasureChest)
    {
	    var treasureChestChance = disregardTreasureChest ? 0.0 : TreasureChestChance;
	    var rainLootTable = Resources.LootTables["rain"];
	    var waterTrashLootTable = Resources.LootTables["water_trash"];

	    var adjustedChances = new List<(string Name, int Tier, double DefaultChance, double InRainChance)>();
	    foreach (var (itemName, chance) in lootTable.Chances)
	    {
		    var defaultChance = chance * (1 - WaterTrashChance) * (1 - treasureChestChance);
		    var inRainChance = defaultChance * (1 - RainChance);
		    adjustedChances.Add((itemName, lootTable.Items[itemName].Tier, defaultChance, inRainChance));
	    }
	    adjustedChances.Add(("Treasure Chest", 0, treasureChestChance, treasureChestChance));

	    foreach (var (itemName, chance) in rainLootTable.Chances)
		    adjustedChances.Add((itemName, rainLootTable.Items[itemName].Tier, 0.0,
			    chance * RainChance * (1 - treasureChestChance)));

	    foreach (var (itemName, chance) in waterTrashLootTable.Chances)
	    {
		    var defaultChance = chance * WaterTrashChance * (1 - treasureChestChance);
		    var inRainChance = defaultChance * (1 - RainChance);
		    adjustedChances.Add((itemName, waterTrashLootTable.Items[itemName].Tier, defaultChance, inRainChance));
	    }

	    return adjustedChances;
    }

    public static (double DefaultChance, double InRainChance) CalculateWormsErrorRates(LootTable lootTable)
    {
	    var dropRates = CalculateDropRates(lootTable, Lure.Bare, true, false);
	    var defaultChanceHighTier = 0.0;
	    var rainChanceHighTier = 0.0;
	    foreach (var entry in dropRates.Where(entry => entry.Item.Tier >= 2))
	    {
		    defaultChanceHighTier += entry.DefaultChance;
		    rainChanceHighTier += entry.InRainChance;
	    }

	    return (Math.Pow(defaultChanceHighTier, 20), Math.Pow(rainChanceHighTier, 20));
    }

    public static List<(Item Item, double DefaultChance, double InRainChance)> CalculateDropRates(
	    LootTable lootTable,
	    Lure lure,
	    bool disregardTreasureChests,
	    bool inVoidZone)
    {
	    RollPicker rollPicker = lure switch
	    {
		    Lure.Bare => RollPickers.PickNormal,
		    Lure.Small => RollPickers.PickSmallest,
		    Lure.Large => RollPickers.PickLargest,
		    Lure.Sparkling => RollPickers.PickHighestTier,
		    Lure.Gold => RollPickers.PickRarest,
		    _ => RollPickers.PickNormal,
	    };

	    var voidChance = inVoidZone ? VoidAltTypeChance : 0.0;
	    var waterTrashChance = lure == Lure.Attractive ? AttractiveWaterTrashChance : WaterTrashChance;
	    var treasureChestChance = lure == Lure.Attractive ? AttractiveTreasureChestChance : TreasureChestChance;
	    if (disregardTreasureChests)
		    treasureChestChance = 0.0;

	    var rainLootTable = Resources.LootTables["rain"];
	    var waterTrashLootTable = Resources.LootTables["water_trash"];

	    var adjustedChances = new List<(Item Item, double DefaultChance, double InRainChance)>();
	    var lootJoinedChances =
		    lootTable.Items
			    .Join(lootTable.Chances, i => i.Key, o => o.Key, (i, o) => (i.Value, o.Value))
			    .ToList();

	    // add chances for all normal items in the lootTable for not-in-rain and in-rain
	    var pickedChances = lootJoinedChances.ToDictionary(j => j.Item1.ItemName, _ => (Default: 0.0, Rain: 0.0));
	    foreach (var (itemA, rollChanceA) in lootJoinedChances)
	    foreach (var (itemB, rollChanceB) in lootJoinedChances)
	    foreach (var (itemC, rollChanceC) in lootJoinedChances)
	    {
		    var pickedRoll = rollPicker.Invoke([itemA, itemB, itemC]);

		    var chance = rollChanceA * rollChanceB * rollChanceC;
		    var defaultChance = chance * (1 - waterTrashChance) * (1 - treasureChestChance) * (1 - voidChance);
		    // non-rain fish are less likely to be caught in rain
		    var inRainChance = defaultChance * (1 - RainChance);

		    var pickedAccumulator = pickedChances[pickedRoll.ItemName];
		    pickedAccumulator.Default += defaultChance;
		    pickedAccumulator.Rain += inRainChance;
		    pickedChances[pickedRoll.ItemName] = pickedAccumulator;
	    }

	    adjustedChances.AddRange(pickedChances.Select(pc =>
		    (lootTable.Items[pc.Key], pc.Value.Default, pc.Value.Rain)));

	    // in void zones, the Void Loot Table can be alternately picked in place of Ocean or Lake loot tables.
	    // however, there is only one fish in the Void Loot Table, so we just add its chance directly
	    {
		    var defaultChance = voidChance * (1 - treasureChestChance) * (1 - waterTrashChance);
		    var inRainChance = defaultChance * (1 - RainChance);
		    adjustedChances.Add((Resources.VoidFish, defaultChance, inRainChance));		    
	    }

	    lootJoinedChances =
		    waterTrashLootTable.Items
			    .Join(waterTrashLootTable.Chances, i => i.Key, o => o.Key, (i, o) => (i.Value, o.Value))
			    .ToList();
	    pickedChances = waterTrashLootTable.Chances.ToDictionary(kvp => kvp.Key, _ => (Default: 0.0, Rain: 0.0));
	    foreach (var (itemA, rollChanceA) in lootJoinedChances)
	    foreach (var (itemB, rollChanceB) in lootJoinedChances)
	    foreach (var (itemC, rollChanceC) in lootJoinedChances)
	    {
		    var pickedRoll = rollPicker.Invoke([itemA, itemB, itemC]);

		    var chance = rollChanceA * rollChanceB * rollChanceC;
		    var defaultChance = chance * waterTrashChance * (1 - treasureChestChance);
		    // non-rain fish are less likely to be caught in rain
		    var inRainChance = defaultChance * (1 - RainChance);

		    var pickedAccumulator = pickedChances[pickedRoll.ItemName];
		    pickedAccumulator.Default += defaultChance;
		    pickedAccumulator.Rain += inRainChance;
		    pickedChances[pickedRoll.ItemName] = pickedAccumulator;
	    }

	    adjustedChances.AddRange(pickedChances.Select(pc =>
		    (waterTrashLootTable.Items[pc.Key], pc.Value.Default, pc.Value.Rain)));

	    lootJoinedChances =
		    rainLootTable.Items
			    .Join(rainLootTable.Chances, i => i.Key, o => o.Key, (i, o) => (i.Value, o.Value))
			    .ToList();
	    pickedChances = rainLootTable.Chances.ToDictionary(kvp => kvp.Key, _ => (Default: 0.0, Rain: 0.0));
	    foreach (var (itemA, rollChanceA) in lootJoinedChances)
	    foreach (var (itemB, rollChanceB) in lootJoinedChances)
	    foreach (var (itemC, rollChanceC) in lootJoinedChances)
	    {
		    var pickedRoll = rollPicker.Invoke([itemA, itemB, itemC]);

		    var chance = rollChanceA * rollChanceB * rollChanceC;
		    var inRainChance = chance * RainChance * (1 - treasureChestChance);

		    var pickedAccumulator = pickedChances[pickedRoll.ItemName];
		    pickedAccumulator.Rain += inRainChance;
		    pickedChances[pickedRoll.ItemName] = pickedAccumulator;
	    }

	    adjustedChances.AddRange(pickedChances.Select(pc =>
		    (rainLootTable.Items[pc.Key], pc.Value.Default, pc.Value.Rain)));

	    

	    // treasure chests override all other picks if selected, with a flat random check
	    adjustedChances.Add((Resources.TreasureChest, treasureChestChance, treasureChestChance));

	    return adjustedChances;
    }

    /*
var totalWeight = items.Sum(l => l.LootWeight);
var maxNameWidth = items.MaxBy(l => l.ItemName.Length).ItemName.Length + 2;
foreach (var item in items.OrderBy(i => i.LootWeight))
    Console.WriteLine($"{item.ItemName.PadRight(maxNameWidth)} {item.LootWeight / totalWeight:P}");

var totalPicks = items.Length * items.Length * items.Length;
var pickCounts = items.ToDictionary(i => i.ItemName, _ => new RollCounter());
foreach (var a in items)
foreach (var b in items)
foreach (var c in items)
{
    var pick = a;
    if (b.Tier > pick.Tier)
        pick = b;
    if (c.Tier > pick.Tier)
        pick = c;

    var aChance = a.LootWeight / totalWeight;
    var bChance = b.LootWeight / totalWeight;
    var cChance = c.LootWeight / totalWeight;
    var value = pickCounts[pick.ItemName];
    value.Probabilities.Add(aChance * bChance * cChance);
    pickCounts[pick.ItemName] = value;
}

foreach (var (itemName, rollCounter) in pickCounts.OrderBy(p => p.Value.Probabilities.Sum()))
{
    var chanceTotal = rollCounter.Probabilities.Sum();
    Console.WriteLine($"{itemName}: {chanceTotal:P}");
}*/
}