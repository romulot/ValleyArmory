# Changelog

## 0.1.3

- Fixed the Prismatic Trial so it no longer depends on another mod to provide its target monsters.
- Added one host-authoritative Iridium Golem per eligible regular-mine floor from 81 through 119
  while the Prismatic Trial is active, limited to once per floor per in-game day.
- Prevented trial spawns during events, on blocked tiles, near players, or when an Iridium Golem is
  already present.
- Changed the Prismatic Trial duration from `Week` to `Month`, so it expires at the start of the
  next season instead of the next week.
- Added automated coverage for spawn eligibility, daily floor deduplication, and the quest target.

## 0.1.0

Initial release.

- 7 custom weapons (Miner's Blade, Black Iron Sword, Prismatic Blade, Shadow Fang, Moon Dagger,
  Stonebreaker, Abyss Hammer), 3 custom boots, and 3 custom armor shirts.
- Rarity system (Common, Rare, Epic, Legendary) with tooltip color and optional weapon lighting.
- Adventurer's Guild shop acquisition, gated by mine depth.
- Monster drop acquisition for a subset of equipment.
- Crafting acquisition, with mine-depth-gated recipe unlocks for higher-tier items.
- Prismatic Trial: an endgame Special Order (mine level 120, 15 Iridium Golems) that rewards the
  Prismatic Blade by mail.
- `va_list`/`va_give` developer console commands.
- English and Brazilian Portuguese localization.
