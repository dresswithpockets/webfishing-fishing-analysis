namespace WebFishingFishWeighting;

public record Item(
    string ItemName,
    string Category,
    int Tier,
    double CatchDifficulty,
    double CatchSpeed,
    string LootTable,
    double LootWeight,
    double AverageSize,
    int SellValue,
    int ObtainXp,
    bool Rare)
{
    public static async Task<Item> FromResource(StreamReader stream)
    {
        var itemName = "";
        var category = "";
        var tier = 0;
        var catchDifficulty = 0.0;
        var catchSpeed = 0.0;
        var lootTable = "";
        var lootWeight = 0.0;
        var averageSize = 0.0;
        var sellValue = 0;
        var obtainXp = 0;
        var rare = false;

        while (!stream.EndOfStream)
        {
            var line = await stream.ReadLineAsync();
            if (line == null || !line.Contains('=') || line.StartsWith('[')) continue;

            var items = line.Split('=');
            var key = items[0].Trim();
            var value = items[1].Trim();
            switch (key)
            {
                case "item_name":
                    itemName = value.Trim('"');
                    break;
                case "category":
                    category = value.Trim('"');
                    break;
                case "tier":
                    tier = int.Parse(value);
                    break;
                case "catch_difficulty":
                    catchDifficulty = double.Parse(value);
                    break;
                case "catch_speed":
                    catchSpeed = double.Parse(value);
                    break;
                case "loot_table":
                    lootTable = value.Trim('"');
                    break;
                case "loot_weight":
                    lootWeight = double.Parse(value);
                    break;
                case "average_size":
                    averageSize = double.Parse(value);
                    break;
                case "sell_value":
                    sellValue = int.Parse(value);
                    break;
                case "obtain_xp":
                    obtainXp = int.Parse(value);
                    break;
                case "rare":
                    rare = bool.Parse(value);
                    break;
            }
        }

        return new Item(itemName, category, tier, catchDifficulty, catchSpeed, lootTable, lootWeight, averageSize,
            sellValue, obtainXp, rare);
    }
}

