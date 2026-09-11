using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace ValleyArmory.Acquisition;

/// <summary>Creates the vanilla Iridium Golem variant without relying on the Wilderness Farm constructor gate.</summary>
internal static class IridiumGolemFactory
{
    private static readonly MethodInfo ParseMonsterInfo = AccessTools.Method(
        typeof(Monster),
        "parseMonsterInfo",
        new[] { typeof(string) }
    ) ?? throw new MissingMethodException("Monster.parseMonsterInfo(string) was not found.");

    public static RockGolem Create(Vector2 position)
    {
        RockGolem golem = new(position);

        // The only public Iridium Golem constructor is gated to Wilderness Farm and a random roll.
        // Reapply the vanilla monster record, then preserve RockGolem's characteristic behavior.
        ParseMonsterInfo.Invoke(golem, new object[] { MonsterIdentifiers.IridiumGolem });
        golem.Name = MonsterIdentifiers.IridiumGolem;
        golem.reloadSprite();
        golem.IsWalkingTowardPlayer = false;
        golem.Slipperiness = 3;
        golem.HideShadow = true;
        golem.jitteriness.Value = 0;
        golem.Sprite.currentFrame = 16;
        golem.Sprite.loop = false;
        golem.Sprite.UpdateSourceRect();

        return golem;
    }
}
