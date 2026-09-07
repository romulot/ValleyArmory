# Valley Armory — especificação de balanceamento

## Proveniência da auditoria F6-0

Os dados vanilla abaixo foram carregados diretamente dos XNB da instalação Stardew Valley `1.6.15.24356` usando `Microsoft.Xna.Framework.Content.ContentManager` e os tipos `WeaponData`/`Dictionary<string, string>` do assembly instalado.

| Fonte | SHA-256 | Tamanho |
|---|---|---:|
| `Content/Data/Weapons.xnb` | `1d7f1c2ce413bb92b28d7282c0f288dec351b05d317efaf778f30e30e671579e` | 2738 bytes |
| `Content/Data/Boots.xnb` | `f6880fb49cf5ccbddd0f30487ff2d278ac02688bf557838f04c4d2e0b958f52f` | 1000 bytes |
| `Stardew Valley.dll` | `f3e97f01d3fd2b1e6094fc8d2b59950aa6cb9d6cd1bf1b39d72d58edda8aad12` | — |

Os valores desta seção são evidência de referência. Nenhum número do catálogo foi alterado nesta etapa.

## Tipos internos confirmados

Campos estáticos de `StardewValley.Tools.MeleeWeapon` no assembly alvo:

| Campo vanilla | Valor | Uso observado |
|---|---:|---|
| `stabbingSword` | 0 | espada de estocada, como Galaxy Sword e Cutlass |
| `dagger` | 1 | adaga |
| `club` | 2 | clava/martelo |
| `defenseSword` | 3 | espada com comportamento defensivo, como Rusty Sword e Claymore |

Conclusão: `Sword` não corresponde a um único valor. O catálogo atual usa `Type: 3` para a Miner's Blade, decisão que deve ser reavaliada quando a factory genérica for implementada. Dagger é `1` e Hammer/Club é `2`.

## Comparáveis vanilla — armas

`CritChance` é fração interna; `0.02` significa 2%. `Price` não existe como campo em `WeaponData`; os preços Valley Armory continuam sendo apenas valores do catálogo.

| Vanilla | Tipo | Dano | Speed | Def | Crit | Mult. | Knockback | Precision | AoE | CanLose | Mine base/min |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|
| Rusty Sword | 3 | 2–5 | 0 | 0 | 0.02 | 3.0 | 1.0 | 0 | 0 | sim | -1/-1 |
| Iron Edge | 3 | 12–25 | -4 | 1 | 0.02 | 3.0 | 1.2 | 0 | 0 | sim | 44/-1 |
| Claymore | 3 | 20–32 | -8 | 2 | 0.02 | 3.0 | 1.3 | 0 | 0 | sim | 86/50 |
| Cutlass | 0 | 9–17 | 4 | 0 | 0.02 | 3.0 | 1.0 | 0 | 0 | sim | -1/-1 |
| Galaxy Sword | 0 | 60–80 | 8 | 0 | 0.02 | 3.0 | 1.0 | 0 | 0 | não | -1/-1 |
| Crystal Dagger | 1 | 4–10 | 0 | 0 | 0.03 | 4.0 | 1.0 | 10 | 0 | sim | -1/-1 |
| Shadow Dagger | 1 | 10–20 | 0 | 0 | 0.04 | 3.0 | 0.5 | 0 | 0 | sim | 80/50 |
| Galaxy Dagger | 1 | 30–40 | 3 | 0 | 0.02 | 3.0 | 1.0 | 0 | 0 | sim | -1/-1 |
| Iridium Needle | 1 | 20–35 | 0 | 0 | 0.10 | 7.0 | 0.5 | 0 | 0 | sim | -1/-1 |
| Wood Club | 2 | 9–16 | -8 | 0 | 0.02 | 3.0 | 1.5 | 0 | 0 | sim | 32/-1 |
| Wood Mallet | 2 | 15–24 | -4 | 0 | 0.02 | 3.0 | 1.3 | 1 | 0 | sim | 68/50 |
| Galaxy Hammer | 2 | 70–90 | -4 | 0 | 0.02 | 3.0 | 1.0 | 0 | 0 | sim | -1/-1 |

`AreaOfEffect` aparece em armas vanilla específicas, mas não deve ser inferido por raridade. O Valley Armory precisa declarar esse campo explicitamente quando a factory genérica for criada.

## Comparáveis vanilla — botas

O XNB atual fornece registros brutos com **7 campos**, separados por `/`:

`Name / Description / Price / Defense / Immunity / ColorIndex / DisplayName`

| ID | Name | Price | Defense | Immunity | ColorIndex | DisplayName |
|---:|---|---:|---:|---:|---:|---|
| 506 | Leather Boots | 50 | 1 | 1 | 2 | Leather Boots |
| 508 | Combat Boots | 150 | 3 | 0 | 4 | Combat Boots |
| 512 | Firewalker Boots | 250 | 3 | 3 | 8 | Firewalker Boots |
| 513 | Genie Shoes | 250 | 1 | 6 | 9 | Genie Shoes |
| 514 | Space Boots | 450 | 4 | 4 | 10 | Space Boots |
| 854 | Mermaid Boots | 1000 | 5 | 8 | 16 | Mermaid Boots |
| 855 | Dragonscale Boots | 1000 | 7 | 0 | 17 | Dragonscale Boots |
| 878 | Crystal Shoes | 1000 | 3 | 5 | 18 | Crystal Shoes |

Texture, sprite sheet e source rectangle são resolvidos pelo `BootsDataDefinition`; eles não aparecem como campos no registro bruto de `Boots.xnb`. A auditoria anterior que descrevia dez campos posicionais estava incorreta para esta instalação e foi corrigida aqui.

## Comparação dos dez itens planejados

| Valley Armory | Papel planejado | Comparáveis | Observação atual |
|---|---|---|---|
| Miner's Blade | espada equilibrada/defesa leve | Iron Edge, Claymore | 14–22, defesa 1; faixa intermediária coerente |
| Black Iron Sword | espada pesada/defensiva | Claymore, Iron Edge | 20–30, defesa 2; velocidade -2 é menos pesada que Claymore |
| Prismatic Blade | lendária, sidegrade endgame | Galaxy Sword, Infinity Blade | 55–72 abaixo de Galaxy/Infinity; preserva espaço de sidegrade |
| Shadow Fang | adaga rápida/crítico | Crystal Dagger, Shadow Dagger | 9–15 e crítico 8%; exige revisão do efeito efetivo de adagas |
| Moon Dagger | velocidade/crítico especializada | Galaxy Dagger, Iridium Needle | crítico 12% e speed 5 superam os comparáveis comuns; risco alto de excesso |
| Stonebreaker | dano/knockback, lenta | Wood Mallet, Galaxy Hammer | 24–38, knockback 1.5; papel intermediário claro |
| Abyss Hammer | dano alto/pesado | Galaxy Hammer, Infinity Gavel | 48–68 abaixo de Galaxy Hammer; speed -6 torna-o mais pesado |
| Miner's Boots | equilibrada | Leather Boots, Combat Boots | defesa 1/imunidade 1; equivalente inicial |
| Obsidian Boots | defesa alta | Combat Boots, Space Boots | defesa 3/imunidade 1; perfil defensivo coerente |
| Ethereal Boots | imunidade alta | Genie Shoes, Crystal Shoes | defesa 2/imunidade 5; forte em imunidade, mas abaixo de Mermaid em defesa |

Recomendações, sem aplicação nesta fase:

- Confirmar se `Black Iron Sword` deve usar tipo 0 ou 3 antes da factory.
- Reavaliar `Moon Dagger` contra a combinação vanilla de velocidade e crítico, pois `CritChance: 0.12` é superior aos exemplos extraídos.
- Manter `Prismatic Blade` como sidegrade, não como substituta da Infinity Blade.
- Corrigir a descrição textual da Prismatic Blade: ela é `Legendary`, não `Rare`.
- Não aplicar multiplicadores automáticos por raridade.

## Unidades e apresentação vanilla

- `CritChance` permanece fração interna.
- `Speed`, `CritChance`, `CritMultiplier`, `Knockback`, `Precision` e `AreaOfEffect` são campos explícitos de `WeaponData`.
- `CanBeLostOnDeath`, `MineBaseLevel` e `MineMinLevel` também são dados do registro e não devem ser derivados da raridade.
- O preço de armas continua fora de `WeaponData`; o valor do catálogo é metadado do Valley Armory até a fase de economia/aquisição.
