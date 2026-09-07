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

## Nota de fechamento — Fase 6A

As sete armas planejadas nesta tabela foram implementadas com os valores de
stats já vigentes em `assets/armory.json`, sem multiplicador automático por
raridade. Nenhum número de balanceamento foi alterado nesta etapa de
consolidação.

O balanceamento atual permanece **provisório** até um playtest mais amplo,
incluindo a recomendação já registrada de reavaliar `Moon Dagger` (crítico
`0.12`) contra os comparáveis vanilla extraídos acima.

## Nota de fechamento — Fase 6B (botas)

As três botas (Miner's Boots, Obsidian Boots, Ethereal Boots) foram
implementadas com os stats já vigentes em `assets/armory.json` (Defense/
Immunity/Price), sem multiplicador automático por raridade — mesma regra já
aplicada às armas. Nenhum valor de balanceamento foi alterado nesta etapa.

O `ColorIndex` de cada bota (paleta vanilla `shoeColors.xnb`, índices 0-18) é
uma escolha de identidade visual, não de balanceamento: Miner's Boots usa 3
(Work Boots), Obsidian Boots usa 7 (Dark Boots), Ethereal Boots usa 9 (Genie
Shoes). Ver `docs/vertical-slice.md` (seção Fase 6B) para a limitação
deliberada de não ter uma textura de recolor customizada nesta fase.

## Nota de fechamento — Fase 6C (armaduras)

As três armaduras (Miner's Armor, Obsidian Armor, Ethereal Armor) foram
adicionadas como `Shirt` (`Data/Shirts`), sem `Defense`/`Immunity`/nenhum
stat de combate — `ShirtData` não tem esses campos, e nenhum patch Harmony
foi criado para simulá-los nesta fase. Os preços (350g/850g/2100g) seguem a
mesma lógica já usada para armas e botas: valor explícito por item,
informado por raridade e por preços vanilla comparáveis de shirts (a maioria
das 303 entradas vanilla fica abaixo de 50g; nossos valores refletem o
material "reforçado"/"élfico" da identidade do mod, não uma tradução literal
do preço vanilla). Continua provisório até playtest mais amplo, e sem
qualquer multiplicador automático por raridade.

## Nota de fechamento — Fase 7A (aquisição via loja)

Os preços de `stats.price` de todos os 13 equipamentos (incluindo os das
armaduras da Fase 6C: 350g/850g/2100g) foram reauditados nesta fase como
candidatos a preço de venda na Adventurer's Guild (`AdventureShop`) e
considerados razoáveis — nenhum valor foi alterado. O `stats.price` agora é
reaproveitado diretamente como o `Price` da entrada de loja (`ShopItemData`),
sem um campo duplicado. Condição de progressão usada:
`MINE_LOWEST_LEVEL_REACHED` em 10/40/80 conforme raridade (Common/Rare/
Epic), reproduzindo patamares já usados pelo próprio `AdventureShop`
vanilla para itens de força comparável (ver `docs/signature-audit.md` e
`docs/vertical-slice.md`, seção Fase 7A). `Prismatic Blade` (Legendary)
permanece fora da loja por decisão de design, não por limitação técnica.

## Fase 8 — balanceamento e integração final

### Proveniência da auditoria F8

Preços reais de venda dos ingredientes de crafting foram extraídos de
`Data/Objects` (tipado como `Dictionary<string, StardewValley.GameData.Objects.ObjectData>`
em 1.6) via `LocalizedContentManager`, na mesma instalação `1.6.15.24356`
já usada nas fases anteriores. `Price` é o campo de venda vanilla do
`ObjectData`.

| ID | Nome | Price (venda) |
|---:|---|---:|
| 334 | Copper Bar | 60 |
| 335 | Iron Bar | 120 |
| 337 | Iridium Bar | 1000 |
| 343 | Stone | 0 |
| 382 | Coal | 15 |
| 768 | Solar Essence | 40 |
| 769 | Void Essence | 50 |
| 848 | Cinder Shard | 50 |
| 910 | Radioactive Bar | 3000 |

Esses valores são usados abaixo apenas como um proxy de "valor em ouro dos
materiais", para comparar o custo de craftar contra o preço de loja — não
representam o tempo/risco reais de obter cada material.

### Problemas encontrados e ajustes

**Problema 1 — armas Common mais caras que armas Rare.**
Evidência: `Black Iron Sword` (Common, 1100g) e `Stonebreaker` (Common,
1200g) custavam mais na loja que `Miner's Blade` (Rare, 900g) e
`Shadow Fang` (Rare, 1000g), apesar de as duas Rare desbloquearem só na
mina 40 contra a mina 10 das Common. Isso invertia o sinal econômico
esperado (raridade/progressão mais tardia deveria custar igual ou mais, não
menos) e não acontecia com botas nem armaduras, onde a ordem já era
crescente por raridade.
Risco: nenhum motivo real para escolher a rota Rare além de velocidade/crit
marginal; a Common vira estritamente a melhor compra.
Ajuste: `Miner's Blade` 900→**1400**; `Shadow Fang` 1000→**1300**. Isso
recoloca a ordem Common (1100/1200) < Rare (1300/1400) < Epic (2800/4000),
igual ao padrão já usado em botas e armaduras.

**Problema 2 — Black Iron Sword vs Miner's Blade (ponto de atenção pedido
explicitamente).**
Evidência: mesmo após o ajuste de preço, `Black Iron Sword` continua com
dano bruto (20–30) e defesa (2) maiores que `Miner's Blade` (14–22, defesa
1). Análise: ambas usam o mesmo `weaponBehavior` (`defenseSword`), mas
representam arquétipos documentados desde a Fase 6A — Black Iron Sword
"pesada/defensiva", Miner's Blade "equilibrada/leve" — e Miner's Blade
compensa com velocidade (+1 vs -2) e crítico (0.03 vs 0.02) maiores. Com o
preço agora corretamente acima da Common e o desbloqueio na mina 40 (contra
mina 10), o sinal econômico e de progressão já justifica a diferença; a
diferença de dano/defesa que resta é um sidegrade de arquétipo deliberado
(ver seção "Sidegrades"), não um erro.
Ajuste: nenhuma mudança de stats de combate — apenas o preço (Problema 1).

**Problema 3 — crafting brutalmente mais barato que a loja (Stonebreaker,
Miner's Armor).**
Evidência: `Stonebreaker` custava ~330g em materiais (Stone é gratuito,
Price=0) contra 1200g na loja (72% de desconto); `Miner's Armor` custava
540g em materiais (Iron Bar x3 + Copper Bar x3) contra apenas 350g na loja —
ou seja, craftar custava **mais** que comprar, o oposto do esperado.
Ajuste: `Stonebreaker` ganhou Iron Bar x5 na receita (330g→930g em
materiais, 22,5% de desconto sobre 1200g). `Miner's Armor` teve Iron Bar
3→2 e Copper Bar 3→1 (540g→300g em materiais, 14,3% de desconto sobre
350g). Ambos ficam agora na mesma faixa de desconto (~15–25%) já
observada em `Black Iron Sword` (930g materiais / 1100g loja, 15,5%).

**Problema 4 — Iridium Bar e Radioactive Bar tornam o crafting de botas e
armaduras Rare/Epic muito mais caro que a loja.**
Evidência: `Obsidian Boots` custava 2250g em materiais (Cinder Shard x5 +
**2** Iridium Bar a 1000g cada) contra 900g na loja; `Obsidian Armor`,
1400g contra 850g; `Ethereal Boots`, 3900g (3 Iridium Bar!) contra 2200g;
`Ethereal Armor`, 6720g (**2** Radioactive Bar a 3000g cada) contra 2100g —
o pior caso do catálogo, craftar custava mais de 3× o preço de loja.
Ajuste: reduzir a quantidade do minério mais caro em cada receita e, onde
isso não bastava, subir modestamente o preço de loja:
- `Obsidian Boots`: Iridium Bar 2→1; preço 900→**1300** (materiais 1250g,
  96%).
- `Obsidian Armor`: Cinder Shard 8→4; preço 850→**1250** (materiais 1200g,
  96%).
- `Ethereal Boots`: Iridium Bar 3→1; preço mantido em 2200g (materiais
  1900g, 86%).
- `Ethereal Armor`: Radioactive Bar x2 substituído por **Iridium Bar x1**
  (o mesmo minério já usado em Ethereal Boots, mesma raridade/mesmo
  desbloqueio); preço mantido em 2100g (materiais 1720g, 82%). Um único
  Radioactive Bar (3000g) já custava mais que o item inteiro pronto
  (2100g) — nenhuma quantidade razoável desse ingrediente resolveria o
  problema sem descaracterizar a receita, por isso a substituição foi o
  ajuste mínimo real, não apenas uma redução de quantidade.

**Problema 5 — Moon Dagger com crítico acima do melhor punhal vanilla.**
Evidência (já registrada como pendência desde a Fase 6A):
`CritChance: 0.12` supera o próprio `Iridium Needle` vanilla (0.10), a
adaga com maior chance de crítico do jogo base, apesar de Moon Dagger ser
"Epic" — uma raridade abaixo do teto do próprio catálogo (Legendary).
Ajuste: `critChance` 0.12→**0.10**, empatando com o teto vanilla em vez de
superá-lo. `critMultiplier` (3.5) permanece abaixo do 7.0 do Iridium
Needle, então o dano crítico efetivo de Moon Dagger continua bem inferior.

**Problema 6 — condição de drop ausente em 3 das 8 regras (inconsistência
interna).**
Evidência: `Shadow Fang`, `Moon Dagger`, `Abyss Hammer`, `Ethereal Boots` e
`Ethereal Armor` já usavam `condition: MINE_LOWEST_LEVEL_REACHED <mesmo
nível do Shop>` no bloco de drop; `Miner's Boots`, `Obsidian Boots` e
`Obsidian Armor` não tinham nenhuma condição, deixando o drop tecnicamente
disponível mesmo antes do nível de mina correspondente à sua raridade.
Ajuste: adicionada a mesma condição já usada no Shop de cada item —
`Miner's Boots` → mina 10, `Obsidian Boots`/`Obsidian Armor` → mina 40 —
sem alterar nenhuma chance de drop.

### Itens revisados e mantidos sem alteração

- `Prismatic Blade`: dano (55–72) fica abaixo do comparável vanilla Galaxy
  Sword (60–80), com mais defesa (2 vs 0) e crítico (0.04 vs 0.02) mas
  menos velocidade (2 vs 8) — sidegrade coerente, não a arma
  numericamente superior do jogo. A missão (`Prismatic Trial`: 15 Iridium
  Golem na mina 120) usa o mesmo monstro já empregado como drop raro do
  Abyss Hammer (mina 80, 2%), reforçando Iridium Golem como referência de
  "monstro de elite" já estabelecida no próprio catálogo — mantido sem
  alteração.
- `Abyss Hammer`: mais lento (-6) e com menos dano médio (58) que o Galaxy
  Hammer vanilla (-4, 80), mas com a maior defesa (4) e o maior knockback
  (1.8) do catálogo — identidade de "clava pesada defensiva", não uma
  cópia inferior do Galaxy Hammer. Mantido.
- `Miner's Boots`: desconto de crafting de 36% (255g materiais / 400g
  loja) é maior que o padrão de ~15% das armas, mas a diferença absoluta
  (145g) é pequena o bastante para não esvaziar a loja nesta faixa de
  preço mais barata do catálogo. Mantido.
- Preços de armaduras (`Miner's Armor` 350g, `Obsidian Armor` 1250g,
  `Ethereal Armor` 2100g): revisados como itens cosméticos/coleção (sem
  stat de combate), mantidos crescentes por raridade e considerados
  razoáveis para o material "reforçado"/"élfico" de cada conjunto.

### Matriz final de aquisição (13 equipamentos)

| Item | Shop | Drop | Crafting | Quest |
|---|---|---|---|---|
| Miner's Blade | sim | não | sim | não |
| Black Iron Sword | sim | não | sim | não |
| Prismatic Blade | não | não | não | sim |
| Shadow Fang | sim | sim | não | não |
| Moon Dagger | sim | sim | não | não |
| Stonebreaker | sim | não | sim | não |
| Abyss Hammer | sim | sim | não | não |
| Miner's Boots | sim | sim | sim | não |
| Obsidian Boots | sim | sim | sim | não |
| Ethereal Boots | sim | sim | sim | não |
| Miner's Armor | sim | não | sim | não |
| Obsidian Armor | sim | sim | sim | não |
| Ethereal Armor | sim | sim | sim | não |

Todos os 13 itens têm ao menos um método de aquisição; `Prismatic Blade` é
o único item Quest-only, por decisão de design (Fase 7D). Nenhum item ficou
com Shop+Drop+Crafting+Quest simultâneos; a distribuição de métodos por
item não mudou nesta fase, apenas os valores dentro de cada método.

### Nota de fechamento — Fase 8

Nenhum ID, alias, sprite, Qualified Item ID, categoria de item ou pipeline
de aquisição foi alterado nesta fase — apenas valores de `stats.price`,
`stats.critChance`, quantidades/identidade de ingredientes de crafting e
condições de drop, todos dentro do `assets/armory.json` já existente. A
raridade continua sendo apenas uma referência de posicionamento, não um
multiplicador automático: cada ajuste acima foi decidido item a item, com
evidência de preço real (`Data/Objects`) ou de dado vanilla real
(`Data/Weapons`/`Data/Boots`, já documentados na Fase 6A/6B), nunca por
fórmula de raridade.
