func _on_fish_catch_timer_timeout():
    # whether or not the local player controls this actor
    if not controlled: return 

    # set the next cycle time for the fish catch timer to a random time between
    # 2 and 3 seconds.
    fish_timer.wait_time = rand_range(2.0, 3.0)
    fish_timer.start()

    # if we're not in the FISHING state, then do nothing and return.
    # the FISHING state is only entered when the player's line-throwing
    # animation has finished and the lure is in the water.
    if state != STATES.FISHING: return

    # variables used to track the chosen Loot Table to roll fish from,
    var fish_type = "ocean"
    # whether or not the chosen fish type can be replaced with another randomly
    # e.g. small chance of replacing the zone fish type with water_trash or rain
    var type_lock = false
    # and, a multiplier applied to the chance that the fish type could be 
    # replaced by water_trash
    var junk_mult = 1.0

    # gets the zone that the player's lure is in and gets some data from it:
    # - fish_type (the loot table)
    # - type_lock (whether or not to try to replace fish_type with water_trash or rain)
    # - junk_mult (chance multipler for getting water_trash)
    # - "boost" (increases the likelihood that you've caught something)
    # - "quality_boost" (multiplier applied to each quality when rolling quality later)
    fish_zone_data = {"id": - 1, "boost": 0.0, "quality_boost": 1.0}
    for zone in fishing_area.get_overlapping_areas():
        if zone.is_in_group("fish_zone"):
            fish_zone_data["id"] = zone.id
            fish_zone_data["boost"] = zone.chance_boost
            fish_zone_data["quality_boost"] = zone.quality_boost
            junk_mult = zone.junk_mult

            if zone.fish_type != "": fish_type = zone.fish_type

            # if alt_chance is above 0, then it rolls to see if it should replace
            # fish_type with the zone's alt_type. This is basically only used for void
            # zones, which have a small chance of replacing the default loot table
            # with the void loot table.
            if zone.alt_chance > 0.0 and randf() < zone.alt_chance:
                fish_type = zone.alt_type

            type_lock = zone.type_lock

    # the game calculates the chances that you've caught something
    var fish_chance = 0.0
    var base_chance = BAIT_DATA[casted_bait]["catch"]
    fish_chance = base_chance
    fish_chance += (base_chance * failed_casts)
    fish_chance += (base_chance * rod_chance)
    fish_chance += fish_zone_data["boost"] * fish_chance
    if recent_reel > 0: fish_chance *= 1.1
    if rod_cast_data == "attractive": fish_chance *= 1.3
    if in_rain: fish_chance *= 1.1

    fish_chance *= catch_drink_boost

    # ...

    # if the player fails the dice roll, then add 0.05 to the failed_casts multiplier and exit the
    # procedure
    if randf() > fish_chance:
        failed_casts += 0.05
        return

    failed_casts = 0.0

    # ...

    # gathers max_tier from the currently selected bait, used later when 
    # rolling items from the loot table
    var max_tier = BAIT_DATA[casted_bait]["max_tier"]

    # ...

    # sets up a variable treasure_mult, which is used later when calculating 
    # the chance that the item you receive is a treasure chest
    var treasure_mult = 1.0
    # then sets junk_mult to 3 and treasure_mult to 2 if you're using the Magnet Lure
    if rod_cast_data == "magnet":
        junk_mult = 3.0
        treasure_mult = 2.0

    # updates your fish_type to use the Ocean or Lake loot tables if you're using
    # the Salty or Fresh lures. Also, note that these only work if the 
    # type_lock flag isn't set, so these lures don't work on Meteors.
    if rod_cast_data == "salty" and not type_lock: fish_type = "ocean"
    if rod_cast_data == "fresh" and not type_lock: fish_type = "lake"

    # force_av_size is a flag used later when calculating the size of the 
    # caught item. If set, then the size calculation will always use the items
    # configured average_size, instead of generating a random size.
    var force_av_size = false

    # determines if the player should get water_trash instead.
    # also gated by type_lock, so doesn't work on Meteors.
    if randf() < 0.05 * junk_mult and not type_lock:
        fish_type = "water_trash"
        max_tier = 0
        force_av_size = true

    # determines if the player s hould get rain fish instead, if the player is
    # in rain. also gated by type_lock
    if in_rain and randf() < 0.08 and not type_lock:
        fish_type = "rain"

    # rolls three items & sizes from the chosen loot table
    var rolls = []
    for i in 3:
        var roll = Globals._roll_loot_table(fish_type, max_tier)
        var s = Globals._roll_item_size(roll)
        rolls.append([roll, s])

    # determines a "reroll type" based on the current lure being used.
    # the reroll_type changes how the game chooses which of the three rolled
    # items should be chosen.
    var reroll_type = "none"
    if rod_cast_data == "small": reroll_type = "small"
    if rod_cast_data == "sparkling": reroll_type = "tier"
    if rod_cast_data == "large": reroll_type = "large"
    if rod_cast_data == "gold": reroll_type = "rare"

    # actually choose from the three rolled items, using the reroll_type.
    # - if Small lure, pick the smallest of the three rolls
    # - if Large lure, pick the largest of the three rolls
    # - if Sparkling lure, pick the highest tier of the three rolls
    # - if Gold lure, pick the last rare-flagged item of the three rolls
    # - otherwise, pick the last of the three rolls
    var chosen = rolls[0]
    for roll in rolls:
        match reroll_type:
            "none":
                chosen = roll
            "small":
                if roll[1] < chosen[1]:
                    chosen = roll
            "large":
                if roll[1] > chosen[1]:
                    chosen = roll
            "tier":
                var old_tier = Globals.item_data[chosen[0]]["file"].tier
                var new_tier = Globals.item_data[roll[0]]["file"].tier
                if new_tier > old_tier:
                    chosen = roll
            "rare":
                var new_rare = Globals.item_data[roll[0]]["file"].rare
                if new_rare:
                    chosen = roll

    # setup some variables with the chosen item and chosen size
    var fish_roll = chosen[0]
    var size = chosen[1]

    # choose a quality based on the current bait selected.
    var quality = PlayerData.ITEM_QUALITIES.NORMAL
    for q in PlayerData.ITEM_QUALITIES.size():
        # if the bait lacks an entry for the current quality we're iterating on
        # then we break out of the loop and use whatever quality has been 
        # selected up to this point
        if BAIT_DATA[casted_bait]["quality"].size() - 1 < q:
            break
        # roll a die, if its within the quality's threshold - taking into 
        # account the zone's quality boost - then, use that quality.
        # as a result of this not breaking when its chosen a quality, this
        # loop always picks the last quality that it successfully rolled for.
        if randf() < (BAIT_DATA[casted_bait]["quality"][q] * fish_zone_data.quality_boost):
            quality = q

    # calculates a small chance and rolls to see if the item caught will 
    # instead be replaced with a Treasure Chest.
    # Doesn't happen when fishing in Meteors.
    if randf() < 0.02 * treasure_mult and not type_lock:
        fish_roll = "treasure_chest"
        size = 60.0
        quality = 0

    # grab the actual item data for the rolled item and the selected quality
    var data = Globals.item_data[fish_roll]["file"]
    var quality_data = PlayerData.QUALITY_DATA[quality]

    # calculate the catch difficulty based on the item
    if force_av_size: size = data.average_size
    var diff_mult = clamp(size / data.average_size, 0.7, 1.8)
    var difficulty = clamp((data.catch_difficulty * diff_mult * quality_data.diff) + quality_data.bdiff, 1.0, 250.0)

    # calculate how much XP you'll get from this catch
    var xp_mult = size / data.average_size
    if xp_mult < 0.15: xp_mult = 1.25 + xp_mult
    xp_mult = max(0.5, xp_mult)
    var xp_add = ceil(data.obtain_xp * xp_mult * catch_drink_xp * quality_data.worth)

    # ...

    # start the actual fishing minigame!!!!
    hud._open_minigame("fishing3", {"fish": fish_roll, "rod_type": rod_cast_data, "reel_mult": catch_drink_reel, "quality": quality, "damage": rod_damage, "speed": rod_spd}, difficulty)
