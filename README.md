# Valley Armory

A data-driven RPG equipment progression mod for Stardew Valley: 7 custom weapons, 3 custom boots and 3
custom armor shirts, acquired through the Adventurer's Guild shop, monster drops, crafting, and a
single endgame quest reward.

## Requirements

- Stardew Valley `1.6.15.24356` (or compatible 1.6.x)
- SMAPI `4.5.2` or later

## Installation

1. Install [SMAPI](https://smapi.io/).
2. Extract the `ValleyArmory` folder into your `Mods` folder (e.g.
   `Stardew Valley/Mods/ValleyArmory`).
3. Launch the game through SMAPI.

## Features

### Equipment

| Category | Items |
|---|---|
| Weapons | Miner's Blade, Black Iron Sword, Prismatic Blade, Shadow Fang, Moon Dagger, Stonebreaker, Abyss Hammer |
| Boots | Miner's Boots, Obsidian Boots, Ethereal Boots |
| Armor (shirts) | Miner's Armor, Obsidian Armor, Ethereal Armor |

Each item has a rarity (Common, Rare, Epic, or Legendary) that drives its tooltip color and, for
weapons, an optional ambient light while equipped.

### Acquisition

Every item (except the Prismatic Blade) is available through at least one of:

- **Adventurer's Guild shop** — gated by mine depth (`MINE_LOWEST_LEVEL_REACHED`).
- **Monster drops** — a small chance from a specific monster, some also gated by mine depth.
- **Crafting** — a recipe with vanilla ingredients, some requiring a mine-depth unlock.

The **Prismatic Blade** is the sole exception: it is never sold, dropped, or craftable. It is
obtained by completing the **Prismatic Trial**, a Special Order that unlocks at mine level 120 and
requires slaying 15 Iridium Golems. Completing it mails the Prismatic Blade to every connected
player.

See `docs/balance-spec.md` for the full stat/price/acquisition rationale.

### Developer commands

- `va_list` — lists all 13 equipment definitions with their alias, rarity, type and qualified item ID.
- `va_give <alias>` — gives one copy of the named equipment to the current player (e.g.
  `va_give miners-blade`). Intended for testing; drops the item on the ground if the inventory is
  full.

## Localization

English (`default`) and Brazilian Portuguese (`pt-BR`) are fully translated.

## Known limitations

- Armor is implemented as a `Shirt` (`Data/Shirts`), not the vanilla `Armor` equipment slot, so it
  has no `Defense`/`Immunity` stat — it is a cosmetic/collectible tier, not a defensive upgrade.
- Multiplayer behavior (drop authority, per-player crafting unlocks, team-wide quest reward) is
  verified through static code analysis of the game's own mechanics, not through a live multi-client
  playtest.
- There is no hot-reload for `assets/armory.json`; changes require restarting the game.

## Contributing / technical documentation

Implementation notes, vanilla-format audits, and the full balance rationale live in `docs/`:

- `docs/vertical-slice.md` — phase-by-phase implementation history.
- `docs/signature-audit.md` — vanilla API/data-format findings that informed the implementation.
- `docs/balance-spec.md` — stat, price, and acquisition balance rationale.
