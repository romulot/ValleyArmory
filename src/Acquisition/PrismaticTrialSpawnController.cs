using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.SpecialOrders;

namespace ValleyArmory.Acquisition;

/// <summary>Adds one Iridium Golem to each eligible mine floor while the Prismatic Trial is active.</summary>
internal sealed class PrismaticTrialSpawnController
{
    private const int MinimumDistanceFromFarmer = 6;
    private readonly IMonitor monitor;
    private readonly PrismaticTrialSpawnTracker tracker = new();
    private bool disabled;

    public PrismaticTrialSpawnController(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.tracker.Clear();
        this.TrySpawn(Game1.currentLocation as MineShaft);
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.tracker.Clear();
        this.TrySpawn(Game1.currentLocation as MineShaft);
    }

    public void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.tracker.Clear();
        this.disabled = false;
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        this.TrySpawn(e.NewLocation as MineShaft);
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(30) || !Context.IsWorldReady || !Context.IsMainPlayer)
        {
            return;
        }

        foreach (MineShaft mine in MineShaft.activeMines.ToArray())
        {
            this.TrySpawn(mine);
        }
    }

    private void TrySpawn(MineShaft? mine)
    {
        if (this.disabled || mine is null || !Context.IsWorldReady)
        {
            return;
        }

        bool questActive = Game1.player.team.specialOrders.Any(order =>
            string.Equals(order.questKey.Value, QuestIdentifiers.PrismaticTrial, StringComparison.Ordinal)
            && order.questState.Value == SpecialOrderStatus.InProgress
        );
        bool alreadyHasTarget = mine.characters.OfType<Monster>().Any(monster =>
            string.Equals(monster.Name, MonsterIdentifiers.IridiumGolem, StringComparison.Ordinal)
        );

        if (!PrismaticTrialSpawnPolicy.IsEligible(
            Context.IsMainPlayer,
            questActive,
            mine.mineLevel,
            mine.currentEvent is not null,
            alreadyHasTarget
        ))
        {
            return;
        }

        Vector2? spawnTile = FindSpawnTile(mine);
        if (spawnTile is null || !this.tracker.TryReserve(Game1.Date.TotalDays, mine.mineLevel))
        {
            return;
        }

        try
        {
            RockGolem golem = IridiumGolemFactory.Create(spawnTile.Value * Game1.tileSize);
            mine.characters.Add(golem);
            this.monitor.Log(
                $"Added a Prismatic Trial Iridium Golem to mine level {mine.mineLevel} at tile {spawnTile.Value}.",
                LogLevel.Trace
            );
        }
        catch (Exception exception)
        {
            this.disabled = true;
            this.monitor.Log(
                $"Prismatic Trial spawns were disabled because an Iridium Golem could not be created: {exception.Message}",
                LogLevel.Error
            );
        }
    }

    private static Vector2? FindSpawnTile(MineShaft mine)
    {
        int width = mine.Map.Layers[0].LayerWidth;
        int height = mine.Map.Layers[0].LayerHeight;
        int start = Game1.random.Next(width * height);

        for (int offset = 0; offset < width * height; offset++)
        {
            int index = (start + offset) % (width * height);
            Vector2 tile = new(index % width, index / width);

            if (!mine.CanSpawnCharacterHere(tile)
                || mine.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action", "Buildings") is not null
                || mine.farmers.Any(farmer => Vector2.Distance(farmer.Tile, tile) < MinimumDistanceFromFarmer))
            {
                continue;
            }

            return tile;
        }

        return null;
    }
}
