# Valley Armory — vertical slice e Fase 6A

## Escopo ativo (Fase 6A)

O catálogo contém e valida dez equipamentos (sete armas e três botas). Nesta
fase, todas as **sete armas** são convertidas e adicionadas a `Data/Weapons`
pelo pipeline genérico. As três botas permanecem apenas reservadas no
catálogo — nenhuma bota é convertida ainda (isso pertence à Fase 6B).

QualifiedItemIds das sete armas:

```text
(W)romulot.ValleyArmory_MinersBlade
(W)romulot.ValleyArmory_BlackIronSword
(W)romulot.ValleyArmory_PrismaticBlade
(W)romulot.ValleyArmory_ShadowFang
(W)romulot.ValleyArmory_MoonDagger
(W)romulot.ValleyArmory_Stonebreaker
(W)romulot.ValleyArmory_AbyssHammer
```

## Mapeamento para WeaponData (pipeline genérico)

Não existe mais uma factory específica por arma. `WeaponDataFactory.Create`
(`src/Assets/WeaponDataFactory.cs`) converte qualquer `EquipmentDefinition` do
tipo `Sword`/`Dagger`/`Hammer` usando o mesmo caminho de código.

| Origem | WeaponData | Observação |
|---|---|---|
| `id` | `Name` | ID permanente namespaced |
| `displayNameKey` | `DisplayName` | tradução ativa |
| `descriptionKey` | `Description` | tradução ativa |
| `weaponBehavior` | `Type` | ver mapeamento abaixo |
| `sprite.assetName` | `Texture` | `Mods/romulot.ValleyArmory/Weapons` (compartilhado pelas 7 armas) |
| `sprite.spriteIndex` | `SpriteIndex` | `0`–`6`, um por arma |
| `stats.*` | campos correspondentes | copiados sem transformação, sem multiplicador por raridade |
| raridade | `CustomFields["romulot.ValleyArmory/Rarity"]` | metadado, nunca altera stats |

### Mapeamento WeaponBehavior → tipo vanilla

Único ponto de tradução no código (`WeaponDataFactory.GetVanillaType`); não há
magic numbers espalhados fora dessa camada:

| WeaponBehavior | Tipo vanilla | Comportamento |
|---|---:|---|
| `StabbingSword` | `0` | espada de estocada (ex.: Galaxy Sword, Cutlass) |
| `Dagger` | `1` | adaga |
| `Club` | `2` | clava/martelo |
| `DefenseSword` | `3` | espada defensiva (ex.: Rusty Sword, Claymore) |

### Semântica vanilla confirmada em Stardew Valley 1.6.15

`MeleeWeapon.ReloadData()` copia `WeaponData.Speed` e `WeaponData.CritChance`
diretamente para os campos de runtime da arma, para qualquer arma do jogo —
esta observação não é específica de nenhum item do Valley Armory.

- Para espadas, a duração-base do golpe é
  `(400 - Speed * 40 - farmer.addedSpeed * 40) * (1 - WeaponSpeedMultiplier)`
  milissegundos.
- O tooltip vanilla exibe a velocidade de espadas como divisão inteira
  `Speed / 2`.
- `CritChance` permanece uma chance-base no combate e é então afetada pelo
  multiplicador de chance crítica dos buffs do jogador.
- O tooltip calcula a unidade exibida como
  `round((CritChance - 0.001) / 0.02)`; esse número não é a porcentagem literal.
- Adagas recebem tratamento vanilla adicional na chance crítica —
  `(CritChance + 0.005) * 1.12` — tanto no combate quanto na apresentação.

Essas fórmulas foram confirmadas por inspeção do IL do assembly instalado
`Stardew Valley.dll`, versão de arquivo `1.6.15.24356`. Elas são comportamento
interno do jogo e devem ser revalidadas ao atualizar a versão-alvo.

Defaults explícitos, aplicados às sete armas:

| WeaponData | Valor | Motivo |
|---|---:|---|
| `Precision` | `0` | catálogo ainda não modela precisão |
| `AreaOfEffect` | `0` | catálogo ainda não modela área adicional |
| `CanBeLostOnDeath` | `true` | preservar comportamento normal de armas |
| `MineBaseLevel` | `-1` | aquisição automática desabilitada |
| `MineMinLevel` | `-1` | drops automáticos desabilitados |

Sem equivalente direto nesta fase:

- `stats.price`: `WeaponData` não possui campo de preço; será usado quando aquisição/economia forem implementadas.
- `acquisition`: permanece apenas metadado `Unspecified`.

## Edição de assets (generalizada)

O handler `Content.AssetRequested` (`AssetInjector.EditWeapons`):

1. carrega `assets/weapons.png` somente quando o asset próprio
   `Mods/romulot.ValleyArmory/Weapons` é solicitado;
2. itera as sete definições de arma do catálogo e edita `Data/Weapons` de
   forma aditiva, uma entrada por vez;
3. verifica se cada ID já existe antes de adicionar (`NonOverwritingAssetEditor.TryAdd`);
4. em colisão de uma arma específica, mantém a entrada existente, registra um
   único `Warning` por ID colidido e **continua processando as demais armas**
   — uma colisão não bloqueia a injeção das outras seis;
5. nunca substitui o dicionário inteiro nem qualquer entrada pré-existente.

## Ferramentas de desenvolvimento (generalizadas)

`va_list` lista exatamente as sete armas, ordenadas por `SpriteIndex`, com
alias, raridade, tipo e QualifiedItemId. Os aliases são derivados
centralizadamente do ID permanente (`DeveloperWeaponCatalog.ToAlias`, kebab-case
a partir do PascalCase) — não há switch duplicado em nenhum outro ponto do
código.

| Alias | QualifiedItemId |
|---|---|
| `miners-blade` | `(W)romulot.ValleyArmory_MinersBlade` |
| `black-iron-sword` | `(W)romulot.ValleyArmory_BlackIronSword` |
| `prismatic-blade` | `(W)romulot.ValleyArmory_PrismaticBlade` |
| `shadow-fang` | `(W)romulot.ValleyArmory_ShadowFang` |
| `moon-dagger` | `(W)romulot.ValleyArmory_MoonDagger` |
| `stonebreaker` | `(W)romulot.ValleyArmory_Stonebreaker` |
| `abyss-hammer` | `(W)romulot.ValleyArmory_AbyssHammer` |

`va_give <alias>` cria a arma via `ItemRegistry.Create`, não aceita botas, faz
o item cair aos pés do jogador se o inventário estiver cheio, e falha com
segurança (mensagem localizada, sem error item) para alias inválido.

## Teste manual no jogo

Pré-condições:

- Stardew Valley `1.6.15`;
- SMAPI `4.5.2`;
- jogo fechado durante a instalação;
- backup normal do save recomendado antes de testar mods em desenvolvimento.

### Instalação

1. Na raiz do repositório, execute `dotnet build`.
2. Localize `bin/Debug/net6.0/ValleyArmory 0.1.0.zip`.
3. Remova qualquer instalação anterior de `Mods/ValleyArmory` e extraia o ZIP
   na pasta `Mods` do Stardew Valley, para não misturar arquivos de builds
   diferentes.

### Execução

1. Inicie o jogo pelo SMAPI e confirme no console a mensagem de carregamento
   do Valley Armory sem erros de catálogo.
2. Abra qualquer save.
3. No console do SMAPI, use `va_list` para conferir as sete armas e
   `va_give <alias>` para qualquer uma delas.

## Fase 4 — decoração de raridade no tooltip (generalizada)

Todas as sete armas (mais as três botas reservadas, quando chegar sua fase)
recebem decoração via o mesmo fluxo:

```text
QualifiedItemId -> EquipmentDefinition -> RarityDefinition -> TooltipPresentation
```

A resolução usa o `QualifiedItemId`, consulta a raridade no catálogo e
converte `RarityDefinition.NameColor` para a cor do título:

| Raridade | Cor do título | Linha de raridade |
|---|---|---|
| Common | mantém a cor vanilla (`NameColor = null`) | exibida |
| Rare | azul (`#3B82F6`) | exibida |
| Epic | roxo (`#9B59B6`) | exibida |
| Legendary | dourado (`#D4A72C`) | exibida |

Itens vanilla ou de outros mods, e as três botas (ainda sem decoração nesta
fase), permanecem inalterados.

Harmony atua nos três pontos confirmados pela auditoria (inalterados desde a
Fase 4 original):

- postfix em `MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons` acrescenta
  uma linha à altura calculada;
- prefix em `MeleeWeapon.drawTooltip` desenha a linha localizada no início da
  área específica da arma e avança `y`, preservando todas as linhas vanilla;
- transpiler em `IClickableMenu.drawHoverText(StringBuilder, ...)` altera somente
  a cor fornecida à chamada principal `SpriteBatch.DrawString` do
  `boldTitleText`.

A altura adicional é `max(48, ceil(font.MeasureString("TT").Y))`. O transpiler
espera exatamente uma correspondência de padrão de IL; zero ou mais de uma
correspondência faz a aplicação falhar. Qualquer falha ao localizar ou aplicar
um dos três patches remove os patches Harmony pertencentes ao mod, desliga
toda a decoração atomicamente e emite um único `Warning`; o tooltip vanilla
permanece disponível para todos os itens.

Essa dependência de IL corresponde ao assembly `1.6.15.24356` e precisa ser
revalidada em atualizações do jogo. Os patches Harmony em si **não foram
alterados** na Fase 6A, apenas a fonte de dados (`TooltipPresentationResolver`)
que já era genérica desde a generalização anterior.

## Fase 5 — iluminação (generalizada, com fix da Prismatic Blade)

Fluxo final:

```text
QualifiedItemId -> EquipmentDefinition -> OptionalVisualOverrides.Light ?? RarityDefinition.Light ?? disabled
```

Estado por raridade:

| Raridade | Armas | Luz |
|---|---|---|
| Common | Black Iron Sword, Stonebreaker | sem luz (`lightEnabled: false`) |
| Rare | Miner's Blade, Shadow Fang | azul fraca (`#4A90E2`, intensity `0.35`) |
| Epic | Moon Dagger, Abyss Hammer | violeta média (`#8E5AC7`, intensity `0.6`) |
| Legendary | Prismatic Blade | override próprio (ver abaixo) |

### Override final da Prismatic Blade (validado manualmente)

```json
{
  "color": "#D63384",
  "radius": 3.00,
  "intensity": 0.75
}
```

A cor original do override (quase branca, `#FCFEFF`/`intensity 1.00`)
renderizava como luz invisível — o `LightSource` do jogo trata cores próximas
do branco como luz muito fraca/nula. O valor final acima produz uma cor de
runtime saturada (magenta escuro) com luminância maior que Epic e Rare, sem se
aproximar do branco. **`ToRuntimeColor()` não foi alterado**; apenas os dados
da Prismatic Blade em `armory.json` foram recalibrados.

O teste de regressão `RuntimeLightColorsIncreaseInLuminanceWithRarityWithoutApproachingWhite`
(`LightingTests.cs`) trava os bytes de runtime color de Miner's Blade e Moon
Dagger e confirma que a luminância cresce Rare → Epic → Legendary sem nenhum
canal se aproximar do branco.

Decisões arquiteturais (inalteradas desde a Fase 5 original, agora aplicadas a
todas as armas com luz):

- Fonte de verdade da luz: `GameLocation.sharedLights`; `Game1.currentLightSources`
  não é manipulado diretamente.
- `radius` nunca influencia cor/brilho — apenas o alcance espacial da luz.
- No máximo uma luz própria ativa por player, com ID determinístico e
  namespaced (`romulot.ValleyArmory/weapon-light/<UniqueMultiplayerID>`).
- Reconciliação idempotente: cria apenas quando necessário, reposiciona/atualiza
  por diferença e remove imediatamente ao perder elegibilidade.
- Fallback isolado: qualquer falha desativa somente o subsistema de iluminação,
  limpa luzes próprias remanescentes e mantém arma/tooltip funcionando.

Eventos de ciclo de vida cobertos: `SaveLoaded`, `DayStarted`, `DayEnding`,
`ReturnedToTitle`, `UpdateTicked`, `Player.Warped`, `PeerConnected`,
`PeerDisconnected`.

### Limitação conhecida (mantida)

> Multiplayer host/farmhand e split-screen ainda não receberam validação
> manual completa in-game. A implementação está fundamentada nas APIs
> auditadas e em testes de lógica pura, mas essa validação permanece
> pendente.

## Fase 6A — estado final e aceite

Critérios de aceite atendidos:

- as sete armas são geradas por um único `WeaponDataFactory` genérico, sem
  factory específica por arma;
- `AssetInjector` injeta cada arma individualmente; colisão em uma não
  bloqueia as demais; nenhuma entrada existente é sobrescrita;
- mapeamento `WeaponBehavior → tipo vanilla` centralizado, sem magic numbers
  espalhados;
- catálogo com exatamente 7 armas e 3 botas, IDs permanentes, tipos e
  raridades corretos, `SpriteIndex` `0`–`6`, stats explícitos, sem
  multiplicador automático por raridade;
- `assets/weapons.png`: 112×16px, 7 células de 16×16 ocupadas, índices `0`–`6`;
- `va_list`/`va_give` funcionam para as sete armas via aliases centralizados;
  botas são rejeitadas;
- tooltip generalizado: Common (cor vanilla), Rare (azul), Epic (roxo),
  Legendary (dourado), linha de raridade em todos, fallback Harmony atômico
  preservado;
- iluminação generalizada: Common sem luz, Rare/Epic conforme raridade,
  Prismatic Blade com override próprio validado manualmente;
- suíte automatizada com **82 testes aprovados**, cobrindo catálogo, factory,
  injeção de assets, sprites, developer tools, tooltip e iluminação
  (incluindo regressão de cor/luminância da Prismatic Blade);
- i18n: chaves idênticas entre `default.json` e `pt-BR.json` (37 chaves em
  cada arquivo).

Pendências conhecidas:

- multiplayer host/farmhand e split-screen da iluminação sem validação manual
  completa (ver acima);
- balanceamento atual (dano, velocidade, crítico, preço) permanece provisório
  até um playtest mais amplo;
- os sprites das sete armas estão funcionais e validados
  tecnicamente/in-game, mas ainda podem receber polish visual futuro;
- botas (`MinersBoots`, `ObsidianBoots`, `EtherealBoots`) permanecem apenas
  reservadas no catálogo — nenhuma delas passa pelo pipeline de
  `Data/Boots`, tooltip ou iluminação nesta fase.

Validação manual já realizada (registrada, não presumida):

- `va_list` e `va_give` funcionam;
- Black Iron Sword criada e exibida corretamente no inventário;
- sprites aparecem no inventário para os itens testados;
- tooltip Epic da Moon Dagger validado visualmente;
- iluminação Rare e Epic testada visualmente;
- Prismatic Blade ajustada manualmente até resultado visual aprovado, com a
  configuração final `#D63384` / `radius 3.00` / `intensity 0.75`.

## Fase 6B — botas

### Escopo

As três botas do catálogo passam a ser convertidas para `Data/Boots` e
injetadas em runtime, com tooltip decorado. Nenhuma delas emite luz.

```text
(B)romulot.ValleyArmory_MinersBoots    — Common
(B)romulot.ValleyArmory_ObsidianBoots  — Rare
(B)romulot.ValleyArmory_EtherealBoots  — Epic
```

### Contrato real de Data/Boots (reauditado)

Ver `docs/signature-audit.md` para o detalhamento completo. Resumo: o formato
vanilla tem 7 campos (`Name/Description/Price/Defense/Immunity/ColorIndex/DisplayName`),
mas o parser aceita até 10 — os 3 extras (não documentados, confirmados via
desmontagem de IL) permitem `SpriteIndex` e nome de textura do ícone próprios
(campos 8 e 9) e, opcionalmente, uma textura de recolor customizada para o
farmer equipado (campo 7). Como o valor inteiro é dividido por `/` pelo
próprio jogo, o campo de textura precisa usar `\` no lugar de `/`.

`BootDataFactory` (`src/Assets/BootDataFactory.cs`) gera os 10 campos a partir
de um `EquipmentDefinition` do tipo `Boots`, preservando os stats do catálogo
sem transformação por raridade, e rejeitando `/` nos campos textuais
traduzíveis (nome/descrição/displayName) — a conversão do nome do asset para
`\` é automática, não depende do autor do catálogo escapar nada manualmente.

### Injeção (Data/Boots)

`BootAssetInjector` (`src/Assets/BootAssetInjector.cs`) espelha `AssetInjector`
mas é um pipeline **separado**: edita `Data/Boots` (não `Data/Weapons`) de
forma aditiva, nunca sobrescreve uma entrada existente, e uma colisão em uma
bota não impede a injeção das outras duas. Também carrega
`assets/boots.png` como `Mods/romulot.ValleyArmory/Boots`.

### Sprites

`assets/boots.png`: 48×16px, 3 células de 16×16 (índices `0`–`2`, um por
bota), silhueta de bota lateral minimalista (cano + pé + sola), 5 cores planas
por célula, sem anti-aliasing, fundo transparente, sem vazamento entre
células — confirmado por `SpriteSheetTests`.

### Tooltip

Reaproveita `TooltipPresentationResolver` (agora também aceita
`EquipmentType.Boots`) e o transpiler de cor do título, que já é genérico para
qualquer `Item` (`IClickableMenu.drawHoverText`). A linha textual de raridade e
o espaço extra do tooltip precisam de patches **próprios** para `Boots`
(`src/Tooltips/BootsTooltipPatches.cs`), pois `Boots.drawTooltip`/
`Boots.getExtraSpaceNeededForTooltipSpecialIcons` são overrides distintos dos
de `MeleeWeapon`, mesmo com a mesma assinatura. `TooltipPatchManager` aplica
os 5 patches (3 de arma + 2 de bota) atomicamente: falha em qualquer um
desfaz todos e desliga a decoração, mantendo o tooltip vanilla.

### Iluminação — ausência intencional

Nenhuma bota emite luz, em nenhuma raridade. Isso já estava garantido pela
própria assinatura do `LightAppearanceResolver`, cujo construtor filtra
`catalog.GetAllEquipment()` por `Sword`/`Dagger`/`Hammer` — `Boots` nunca
entra no dicionário de aparências. Nenhuma mudança de código foi necessária;
`LightingTests` ganhou um teste explícito (`NoBootEverResolvesLightRegardlessOfRarity`)
cobrindo as três botas para travar essa garantia.

### Developer tools

`DeveloperWeaponCatalog`/`DeveloperWeaponEntry` foram renomeados para
`DeveloperEquipmentCatalog`/`DeveloperEquipmentEntry`
(`src/DeveloperTools/DeveloperEquipmentCatalog.cs`) e agora incluem armas e
botas, ordenados deterministicamente por grupo (armas primeiro, depois botas)
e então por `SpriteIndex`. `va_give` aceita os 10 aliases e cria via
`ItemRegistry.Create` tanto `MeleeWeapon` quanto `Boots`.

| Alias | QualifiedItemId |
|---|---|
| `miners-boots` | `(B)romulot.ValleyArmory_MinersBoots` |
| `obsidian-boots` | `(B)romulot.ValleyArmory_ObsidianBoots` |
| `ethereal-boots` | `(B)romulot.ValleyArmory_EtherealBoots` |

### Limitação deliberada: sem `assets/boots-colors.png`

O campo 7 do formato estendido permite uma textura de recolor customizada
para as botas equipadas no sprite do farmer (`Farmer.changeShoeColor`). Esta
fase **não** implementa isso. Em vez de criar uma textura nova sem poder
validar visualmente o alinhamento pixel a pixel com o spritesheet do farmer
(risco de recolor quebrado/vazando para pixels errados, impossível de
verificar sem rodar o jogo), cada bota usa um índice já existente da paleta
vanilla `Characters/Farmer/shoeColors.xnb` (0–18), escolhido por
aproximação de identidade visual:

| Bota | ColorIndex | Referência vanilla |
|---|---:|---|
| Miner's Boots | 3 | Work Boots (utilitária) |
| Obsidian Boots | 7 | Dark Boots (escura/robusta) |
| Ethereal Boots | 9 | Genie Shoes (mística) |

Isso é seguro (reutiliza um recurso vanilla já testado pelo próprio jogo) e
reversível: se uma textura de recolor customizada for aprovada no futuro, ela
se encaixa no campo 7 sem mudar o resto do formato.

### Testes

`BootsPipelineTests.cs` (novo): catálogo com exatamente 3 botas, formato de 10
campos na ordem correta, stats preservados sem transformação por raridade,
rejeição de definição não-Boots (e o inverso: `WeaponDataFactory` rejeita
Boots), rejeição de `/` em campos traduzíveis, QualifiedItemIds `(B)` sem
colisão com `(W)`, edição aditiva/colisão parcial/não-sobrescrita.
`SpriteSheetTests.cs` ganhou 3 testes para `boots.png`. `LightingTests.cs`
ganhou a garantia de ausência de luz. `TooltipPresentationTests.cs` e
`DeveloperToolsTests.cs` foram estendidos para as 3 botas.

### Validação manual pendente (a executar após esta fase)

`va_list`/`va_give miners-boots|obsidian-boots|ethereal-boots`; sprite correto
no inventário; aparência ao equipar (paleta vanilla reaproveitada); Defense/
Immunity corretos; tooltip Common/Rare/Epic; confirmar que nenhuma bota cria
luz; save/reload preserva; inventário cheio dropa aos pés; armas continuam
funcionando sem regressão de tooltip/iluminação.

## Fase 6C — armaduras

### Decisão técnica

Stardew Valley 1.6.15 não tem um item ou slot vanilla chamado "Armor". A
representação real de roupa de corpo é `StardewValley.Objects.Clothing`, que
cobre dois tipos de dado (`Data/Shirts` e `Data/Pants`, ambos fortemente
tipados — `ShirtData`/`PantsData`, com `Texture`/`SpriteIndex` explícitos,
igual a `WeaponData`). Ver `docs/signature-audit.md` para o detalhamento
completo da auditoria.

**Decisão:** cada "Armor" do Valley Armory é implementada como **um único
Shirt** (`Data/Shirts`, QualifiedItemId `(S)`), não como shirt+pants. Motivos:

- um Shirt já é um item vanilla completo e independente — equipar só a
  camisa, sem calça, não quebra nada no jogo;
- `PantsDataDefinition.GetSourceRect` usa uma grade fixa de 192×688px com
  ícones 16×16, mais rígida e sem benefício claro para uma textura pequena e
  dedicada;
- dobrar o pipeline (Shirt + Pants) dobraria também a superfície de auditoria
  e teste para um segundo mecanismo cujo layout de frames equipados também é
  incerto (ver limitação abaixo), sem necessidade concreta pedida nesta fase;
- mantém `va_give miners-armor` simples: um alias, um item, um patch de
  tooltip — sem precisar sincronizar dois itens internos.

Nenhum sistema paralelo de equipamento foi criado. `ShirtData.CustomFields`
guarda a raridade exatamente como `WeaponData`/`ShirtData`.

### Pipeline

```text
EquipmentDefinition (Type: Shirt)
        ↓
ArmorDataFactory.Create()  →  StardewValley.GameData.Shirts.ShirtData
        ↓
ArmorAssetInjector.EditShirts()  (edição aditiva, Data/Shirts)
        ↓
ItemRegistry.Create("(S)romulot.ValleyArmory_MinersArmor", ...)
```

Idêntico em estrutura ao pipeline de armas (`WeaponDataFactory` +
`AssetInjector`), sem factory nem patch específico por item. `Defense`/
`Immunity` customizados não são implementados nesta fase — `ShirtData` não
tem esses campos, e não foi criado nenhum Harmony patch para adicioná-los
(fora de escopo, conforme decidido).

### Armaduras

| Item | Raridade | Preço | Sprite | Combina com |
|---|---|---:|---:|---|
| Miner's Armor | Common | 350g | 0 | Miner's Boots |
| Obsidian Armor | Rare | 850g | 1 | Obsidian Boots |
| Ethereal Armor | Epic | 2100g | 2 | Ethereal Boots |

Nenhuma armadura Legendary foi criada — a Prismatic Blade continua sendo o
único item Legendary. A arquitetura (enum `EquipmentType.Shirt`,
`ArmorDataFactory`, validador) já suporta adicionar uma futura armadura
Legendary sem refatoração: bastaria uma nova entrada no catálogo.

### Tooltip

Diferente de `MeleeWeapon` e `Boots`, `StardewValley.Objects.Clothing` **não
sobrescreve** `drawTooltip`/`getExtraSpaceNeededForTooltipSpecialIcons` —
usa a implementação base de `Item` diretamente (confirmado por reflexão). Por
isso, `ClothingTooltipPatches` (`src/Tooltips/ClothingTooltipPatches.cs`)
aplica seu prefix/postfix na declaração de `Item`, não em `Clothing`. Na
prática isso só afeta itens que o nosso `TooltipPresentationResolver`
reconhece (early-exit para qualquer outro item), mas é um alvo de patch mais
amplo que os anteriores — documentado aqui para quem for depurar
compatibilidade com outros mods de tooltip no futuro. O transpiler de cor do
título (`IClickableMenu.drawHoverText`) já era genérico e cobre armadura sem
mudança. `TooltipPatchManager` aplica os 7 patches (3 arma + 2 bota + 2
armadura) atomicamente — falha em qualquer um desliga a decoração inteira.

### Iluminação — ausência intencional

Nenhuma mudança de código foi necessária: `LightAppearanceResolver` filtra
por `Sword`/`Dagger`/`Hammer` no construtor, então `Shirt` nunca entra no
dicionário de aparências, exatamente como já acontecia com `Boots`.
`LightingTests` ganhou `NoArmorEverResolvesLightRegardlessOfRarity` para
travar essa garantia.

### Assets

`assets/armor.png`: 48×32px. Formato ditado pela fórmula real de
`ShirtDataDefinition.GetSourceRect` (colunas = largura/2 = 24; cada ícone é
8×8 num bloco de 32px de altura). Os 3 ícones reais ficam nas colunas 0/1/2
(spriteIndex 0/1/2); as colunas 3/4/5 (metade direita, não usada quando
`CanBeDyed=false`) recebem cópias espelhadas por precaução. Cada bloco de
32px de altura é preenchido com 4 cópias idênticas do ícone (linhas
0/8/16/24) — mitigação para a incerteza registrada em
`docs/signature-audit.md` sobre quais sub-regiões `FarmerRenderer` amostra ao
desenhar a roupa equipada em diferentes poses. Nenhum canal de anti-aliasing
(alpha estritamente 0 ou 255), silhueta simples de colete/túnica em 8×8,
identidade de cor compartilhada com a bota correspondente (marrom/couro,
quase-preto com brilho arroxeado, lilás/branco pálido).

### Developer tools

`DeveloperWeaponCatalog` já havia sido renomeado para `DeveloperEquipmentCatalog`
na Fase 6B; agora também inclui `Shirt`, com ordenação determinística por
grupo (armas → botas → armaduras) e depois por `SpriteIndex`.

| Alias | QualifiedItemId |
|---|---|
| `miners-armor` | `(S)romulot.ValleyArmory_MinersArmor` |
| `obsidian-armor` | `(S)romulot.ValleyArmory_ObsidianArmor` |
| `ethereal-armor` | `(S)romulot.ValleyArmory_EtherealArmor` |

### Testes

`ArmorPipelineTests.cs` (novo): catálogo com exatamente 3 armaduras,
`ArmorDataFactory` produz `ShirtData` correto (nome/descrição traduzidos,
preço, textura, spriteIndex, raridade em `CustomFields`, `CanBeDyed`/
`IsPrismatic`/`HasSleeves` sempre `false`), stats não derivados de raridade,
rejeição cruzada entre factories (`ArmorDataFactory` rejeita não-Shirt,
`WeaponDataFactory`/`BootDataFactory` rejeitam Shirt), QualifiedItemIds `(S)`
sem colisão, edição aditiva/colisão parcial/não-sobrescrita.
`SpriteSheetTests.cs` ganhou 3 testes para `armor.png` (dimensão, fórmula
real de sprite rect, ausência de anti-aliasing). `TooltipPresentationTests.cs`,
`LightingTests.cs` e `DeveloperToolsTests.cs` foram estendidos para as 3
armaduras. Catálogo e distribuição de raridade atualizados de 10 para 13
itens (Common/Rare/Epic passam de 3 para 4 cada; Legendary continua 1).

### Validação manual pendente (a executar após esta fase)

`va_list`/`va_give miners-armor|obsidian-armor|ethereal-armor`; sprite
correto no inventário; **aparência ao equipar em diferentes direções e
durante caminhada** (a limitação registrada em signature-audit.md sobre
frames amostrados por `FarmerRenderer` só pode ser validada jogando);
combinação visual com a bota correspondente; tooltip Common/Rare/Epic;
confirmar ausência de luz; save/reload preserva; inventário cheio dropa aos
pés; armas e botas continuam funcionando sem regressão.

Próximo passo sugerido logo após a Fase 6C: aquisição via loja (concluído
como **Fase 7A**, ver abaixo). Capacete/pernas dedicados, drops, crafting,
quests e stats customizados de armadura via Harmony seguem em aberto para
fases futuras.

## Fase 7A — aquisição via loja (Adventurer's Guild)

### Decisão técnica

`Data/Shops` vive em um assembly separado (`StardewValley.GameData.dll`),
não em `Stardew Valley.dll` — diferente de `WeaponData`/`ShirtData`. O
identificador real da Adventurer's Guild é `AdventureShop` (dono: Marlon),
confirmado carregando `Data/Shops` de verdade (77 entradas). Detalhes
completos em `docs/signature-audit.md` (seção "Lojas / Adventurer's Guild —
Fase 7A").

`AcquisitionMetadata` ganhou um campo `Source` (enum `Unspecified | Shop |
Drop | Crafting | Quest | Reward`) e um bloco opcional `Shop` (`ShopId`,
`Condition`). Só `Shop` tem implementação real; os demais valores existem
apenas como rótulo declarativo para uso futuro — nenhuma classe ou pipeline
vazio foi criado para eles. O preço de venda reaproveita o `stats.price` já
existente (nenhum campo de preço duplicado).

### Pipeline

```
EquipmentDefinition.Acquisition (Source=Shop, Shop={ShopId, Condition})
        ↓
ShopAcquisitionInjector.ApplyTo(Dictionary<string, ShopData>)
        ↓ (agrupa por ShopId, resolve item existente no dicionário)
ShopData.Items.Add(ShopItemData)  // Id=<equipamento>, ItemId=<Qualified ID>, Price=stats.Price, AvailableStock=-1, Condition=<GSQ ou null>
        ↓
Data/Shops (AdventureShop) — edição aditiva, nunca substitui a loja
```

Diferente de `Data/Boots`/`Data/Shirts` (chave nova em um dicionário
top-level via `NonOverwritingAssetEditor.TryAdd`), aqui a edição é um
**append** à lista `Items` de uma entrada de dicionário já existente
(`AdventureShop`), com isolamento de falha por item (try/catch por
definição) e detecção de colisão por `ShopItemData.Id` dentro da própria
lista.

### Progressão configurada

| Equipamento | Tipo | Raridade | Preço | Condição | Shop |
|---|---|---|---:|---|---|
| Black Iron Sword | Espada | Common | 1100g | `MINE_LOWEST_LEVEL_REACHED 10` | AdventureShop |
| Stonebreaker | Martelo | Common | 1200g | `MINE_LOWEST_LEVEL_REACHED 10` | AdventureShop |
| Miner's Boots | Botas | Common | 400g | `MINE_LOWEST_LEVEL_REACHED 10` | AdventureShop |
| Miner's Armor | Armadura | Common | 350g | `MINE_LOWEST_LEVEL_REACHED 10` | AdventureShop |
| Miner's Blade | Espada | Rare | 900g | `MINE_LOWEST_LEVEL_REACHED 40` | AdventureShop |
| Shadow Fang | Adaga | Rare | 1000g | `MINE_LOWEST_LEVEL_REACHED 40` | AdventureShop |
| Obsidian Boots | Botas | Rare | 900g | `MINE_LOWEST_LEVEL_REACHED 40` | AdventureShop |
| Obsidian Armor | Armadura | Rare | 850g | `MINE_LOWEST_LEVEL_REACHED 40` | AdventureShop |
| Moon Dagger | Adaga | Epic | 2800g | `MINE_LOWEST_LEVEL_REACHED 80` | AdventureShop |
| Abyss Hammer | Martelo | Epic | 4000g | `MINE_LOWEST_LEVEL_REACHED 80` | AdventureShop |
| Ethereal Boots | Botas | Epic | 2200g | `MINE_LOWEST_LEVEL_REACHED 80` | AdventureShop |
| Ethereal Armor | Armadura | Epic | 2100g | `MINE_LOWEST_LEVEL_REACHED 80` | AdventureShop |

Os patamares 10/40/80 não são arbitrários: reproduzem exatamente os níveis
já usados pelo próprio `AdventureShop` vanilla para armas/botas de força
comparável (ex.: `WorkBoots`/`Femur` em 10, `ObsidianEdge`/anéis em 40-45,
`(B)512`/anéis em 80). `AvailableStock = -1` (ilimitado) em todos, também
espelhando o padrão vanilla real observado.

### Equipamento fora da loja

**Prismatic Blade** (Legendary) permanece com `Source: Unspecified` — sem
bloco `Shop`. Venda direta na Guilda tornaria o item lendário trivialmente
acessível via ouro, o que não é coerente com seu papel de sidegrade
endgame; a decisão foi documentar essa ausência em vez de forçar uma
condição artificial só para preencher a tabela. Fica reservado para uma
futura fase de quest/recompensa/drop.

### Preços de armadura (Fase 6C) — preservados

Os preços de Miner's/Obsidian/Ethereal Armor (350g/850g/2100g) foram
auditados nesta fase para uso como preço de venda na Guilda e considerados
razoáveis frente aos comparáveis de `docs/balance-spec.md`; nenhum valor foi
alterado.

### Compatibilidade e isolamento

`AdventureShop.Items` já vem com 40 entradas vanilla; a injeção do Valley
Armory apenas dá `.Add` na lista existente. `ShopItemData.Id` usa o ID
namespaced completo do Valley Armory (não um rótulo curto tipo
`"ElfBlade"`), evitando colisão com qualquer entrada vanilla ou de outro
mod. Uma colisão de `Id` em um item não impede os demais de serem
adicionados (testado em `AcquisitionPipelineTests.cs`).

### Tooltip e developer tools — inalterados

Nenhum preço/condição de loja foi adicionado ao tooltip. `va_list`/`va_give`
continuam ferramentas de desenvolvimento; nenhum alias novo foi criado
apenas por causa da aquisição (os 13 aliases já existentes cobrem os mesmos
equipamentos).

### Testes

`AcquisitionPipelineTests.cs` (novo, 19 testes): validação de definição
(shopId ausente, bloco `shop` ausente com `Source=Shop`, `Source` não-Shop
carregando bloco `shop` indevido, `Source` ausente, preço zero com
aquisição de loja, equipamento sem aquisição de loja permitido), nível de
catálogo (12 de 13 configurados para loja, preços/shopIds explícitos,
Prismatic Blade fora da loja), pipeline (`ShopAcquisitionInjector.BuildEntry`
gera Qualified ID/preço/estoque/condição corretos para arma, bota e
armadura; ausência de condição preservada como sempre-disponível; nenhuma
lógica depende do nome do equipamento), compatibilidade (`ApplyTo` preserva
entradas vanilla, ignora `ShopId` desconhecido sem lançar exceção, isola uma
colisão sem bloquear os demais itens, não toca em outras lojas do
dicionário).

### Validação manual pendente (a executar após esta fase)

Abrir o jogo, carregar um save, visitar a Adventurer's Guild em diferentes
estágios de progresso nas minas (nível < 10, 10-39, 40-79, 80+) e confirmar
que os itens aparecem/desaparecem conforme a condição; comprar pelo menos
uma arma, uma bota e uma armadura e confirmar que funcionam normalmente;
confirmar que itens vanilla da Guilda continuam presentes e sem duplicatas;
confirmar tooltip/raridade/iluminação normais nos itens comprados.

Próximo passo sugerido logo após a Fase 7A: drops de monstro (concluído como
**Fase 7B**, ver abaixo). Crafting, quests e recompensas seguem em aberto.

## Fase 7B — aquisição via drops de monstro

### Decisão técnica

`Data/Monsters` continua no formato legado (`Dictionary<string,string>`,
registro posicional) e seu campo de drops só aceita IDs de objeto simples —
não há como declarar ali um Qualified Item ID. Por isso o Valley Armory
**não edita `Data/Monsters`**; em vez disso usa o ponto de extensão real que
o próprio jogo expõe para esse fim: `Monster.getExtraDropItems()`, um método
virtual chamado uma única vez por `GameLocation.monsterDrop` e já usado
nativamente por Ghost/Bat/Bug/RockGolem/BigSlime para seus drops especiais.
Um único patch Harmony **postfix** nesse método (na implementação base)
cobre os 51 monstros de `Data/Monsters`, incluindo os 5 subtipos especiais,
porque todos eles chamam `base.getExtraDropItems()` internamente (confirmado
por disassembly de IL). Detalhes completos em `docs/signature-audit.md`
(seção "Drops de monstro — Fase 7B").

Harmony foi necessário aqui (diferente da Fase 7A) porque, ao contrário de
`Data/Shops`, não existe um asset de dados que o motor já avalie
nativamente para decidir e materializar esse tipo de drop — a decisão
(condição, chance, criação do item) precisa ser código.

### Evolução do modelo de `Acquisition`

A Fase 7A usava um único campo `Source` (enum) que só permitia UMA forma de
aquisição por equipamento. Para permitir Shop **e** Drop coexistindo no
mesmo item, `Source` foi removido e `AcquisitionMetadata` passou a expor
dois blocos independentes e opcionais, `Shop` e `Drop` — a presença de cada
um é que determina se aquele método está ativo, sem discriminador central.
Migração verificada por teste (`PhaseSevenAShopConfigurationIsPreservedExactlyAfterTheDropMigration`):
os 12 itens de loja mantêm exatamente o mesmo `ShopId`/preço/condição de
antes; `Prismatic Blade` continua sem nenhuma aquisição.

### Pipeline

```
EquipmentDefinition.Acquisition.Drop (SourceType=Monster, SourceId, Chance, Condition?)
        ↓
DropRuleResolver (ILookup<string monsterName, DropRule>)
        ↓
MonsterDropPatches.Postfix(Monster __instance, ref List<Item> __result)
        │  (Harmony postfix em Monster.getExtraDropItems(), chamado por GameLocation.monsterDrop)
        ├── GameStateQuery.CheckConditions(condition, ...)   se condition presente
        ├── Game1.random.NextDouble() < Chance                (DropRuleResolver.RolledSuccess, testável isoladamente)
        └── ItemRegistry.Create(EquipmentIdentity.GetQualifiedItemId(equipment))  → __result.Add(item)
        ↓
GameLocation.monsterDrop converte cada Item em Debris real (pipeline vanilla, sem código nosso)
```

`DropPatchContext` é o mesmo padrão estático de ponte já usado por
`TooltipPatchContext`, necessário porque o método do Harmony precisa ser
estático.

### Drops configurados

| Equipamento | Raridade | Origem | Chance | Condição |
|---|---|---:|---:|---|
| Miner's Boots | Common | Green Slime | 6% | — |
| Shadow Fang | Rare | Shadow Brute | 5% | MINE_LOWEST_LEVEL_REACHED 40 |
| Obsidian Boots | Rare | Lava Crab | 5% | — |
| Obsidian Armor | Rare | Hot Head | 4% | — |
| Moon Dagger | Epic | Skeleton Mage | 2% | MINE_LOWEST_LEVEL_REACHED 80 |
| Abyss Hammer | Epic | Iridium Golem | 2% | MINE_LOWEST_LEVEL_REACHED 80 |
| Ethereal Boots | Epic | Carbon Ghost | 1.5% | MINE_LOWEST_LEVEL_REACHED 80 |
| Ethereal Armor | Epic | Putrid Ghost | 1.5% | MINE_LOWEST_LEVEL_REACHED 80 |

Todos os 8 acima **também** têm Shop (Shop + Drop coexistindo). Black Iron
Sword, Stonebreaker, Miner's Armor e Miner's Blade permanecem só-Shop, por
decisão de escopo (evitar drop em todo o catálogo). Prismatic Blade não tem
Shop nem Drop.

Justificativa temática, não só por raridade: Shadow Brute/Shadow Fang
(sombrio), Lava Crab/Hot Head → Obsidian (Vulcão, tema lava/obsidiana),
Skeleton Mage/Iridium Golem (minas profundas, papel Epic), Carbon
Ghost/Putrid Ghost → Ethereal (variantes fantasmagóricas do Skull Cavern,
combinam com a identidade "etérea" das Fases 6B/6C). As chances (1.5%-6%)
foram escolhidas bem abaixo dos comparáveis vanilla de recursos comuns
(30%-90% para itens como fatias/geodos no mesmo `Data/Monsters`), refletindo
que equipamento é muito mais significativo que um recurso empilhável; o teto
de validação (50%) existe só para impedir um valor absurdo futuro, não
porque algum item chega perto disso.

### Multiplayer e RNG

Nenhuma checagem `IsMainPlayer` foi adicionada: a cadeia real
`takeDamage → damageMonster → onMonsterKilled → monsterDrop →
getExtraDropItems` só executa no cliente do farmer que desferiu o golpe
fatal (confirmado por disassembly, não assumido) — o mesmo modelo que já
protege os drops especiais vanilla contra duplicação. RNG usa `Game1.random`
(mesma fonte usada por `Ghost.getExtraDropItems()`), nunca `new Random()`.

### Testes

`AcquisitionPipelineTests.cs` ganhou a seção de Drop: validação de definição
(`sourceType`/`sourceId`/`chance` obrigatórios, chance acima do teto de 50%
rejeitada, condição em branco rejeitada), catálogo (8 de 13 configurados
para drop, todos com `SourceType=Monster` e `SourceId` num identificador
centralizado conhecido, Shop+Drop coexistindo em Shadow Fang, Prismatic
Blade sem nenhuma aquisição), pipeline (`DropRuleResolver.GetRulesFor`
resolve por monstro sem depender do nome do equipamento, suporta múltiplas
regras no mesmo monstro sem duplicação), RNG (`RolledSuccess` testado com
valores abaixo/no limite/acima da chance, de forma determinística e sem
tocar `Game1.random`), migração (os 12 itens de loja da Fase 7A preservados
byte a byte na configuração).

### Validação manual pendente (a executar após esta fase)

Visitar as minas em profundidades correspondentes às condições configuradas
e derrotar os monstros listados o suficiente para observar pelo menos um
drop de cada; confirmar que o item aparece como debris normal no chão,
pode ser coletado, mantém tooltip/raridade/iluminação corretos; confirmar
que a loja continua funcionando em paralelo; multiplayer/split-screen não
testado manualmente (autoridade confirmada apenas por análise estática).

Próximo passo sugerido logo após a Fase 7B: crafting (concluído como
**Fase 7C**, ver abaixo). Quest e Reward seguem em aberto.

## Fase 7C — aquisição via crafting

### Decisão técnica

`Data/CraftingRecipes` continua no formato legado (`Dictionary<string,string>`,
registro posicional). O ponto crítico da fase — se o crafting vanilla aceita
produzir diretamente um Weapon/Boots/Shirt custom — foi confirmado por
disassembly de `CraftingRecipe.createItem()`/`GetItemData()`: o output é
resolvido via `ItemRegistry.Create(qualifiedItemId, ...)` usando o próprio
Qualified Item ID armazenado no campo de output, sem qualificação adicional
para receitas não-`bigCraftable`. **Nenhum Harmony foi necessário para o
output** — bastou usar `EquipmentIdentity.GetQualifiedItemId` (a mesma
resolução centralizada já usada por Shop/Drop) como o valor desse campo.
Detalhes completos em `docs/signature-audit.md` (seção "Crafting — Fase
7C").

Para o **unlock** da receita, `Farmer.craftingRecipes` é estado por-jogador,
e o único mecanismo data-driven confirmado que funciona tanto para
personagens novos quanto para saves existentes é `Data/TriggerActions` (uma
`List<TriggerActionData>`, não dicionário) com a ação padrão vanilla
`MarkCraftingRecipeKnown`. Isso substitui completamente qualquer
polling/`UpdateTicked` — o gatilho `DayStarted` roda uma vez por dia por
cliente, e a autoridade por-jogador já é resolvida pelo próprio mecanismo
(`HostOnly=False`, confirmado nas 31 entradas vanilla existentes).

### Pipeline

```
EquipmentDefinition.Acquisition.Crafting (Ingredients[], UnlockCondition?)
        ↓
CraftingRecipeInjector.BuildRecipeString      CraftingUnlockInjector.BuildTriggerAction
        ↓                                             ↓
Data/CraftingRecipes                          Data/TriggerActions
"ing1 qty1 ing2 qty2/Home/                    { Id, Trigger="DayStarted",
 (X)equipmentId 1/false/default/"                Condition=UnlockCondition,
        ↓                                        Action="MarkCraftingRecipeKnown Current <id>" }
Crafting Menu (vanilla)                                ↓
        ↓                                     Farmer.craftingRecipes (per-player)
createItem() → ItemRegistry.Create(qualifiedId)
```

Ambos injetores seguem o mesmo padrão aditivo/isolamento de falha dos
injetores de Shop/Drop (`NonOverwritingAssetEditor.TryAdd` para o
dicionário de receitas; checagem de `Id` já existente para a lista de
trigger actions), com `try/catch` por definição para não derrubar as
demais.

### Recipes configuradas

| Equipamento | Raridade | Ingredientes | Unlock | Outros métodos |
|---|---|---|---|---|
| Black Iron Sword | Common | Iron Bar x5, Coal x10, Copper Bar x3 | — (sempre disponível) | Shop |
| Stonebreaker | Common | Stone x50, Coal x10, Copper Bar x3 | — | Shop |
| Miner's Boots | Common | Copper Bar x3, Coal x5 | — | Shop + Drop |
| Miner's Armor | Common | Iron Bar x3, Copper Bar x3 | — | Shop |
| Miner's Blade | Rare | Iron Bar x8, Coal x15, Copper Bar x5 | MINE_LOWEST_LEVEL_REACHED 40 | Shop |
| Obsidian Boots | Rare | Cinder Shard x5, Iridium Bar x2 | MINE_LOWEST_LEVEL_REACHED 40 | Shop + Drop |
| Obsidian Armor | Rare | Cinder Shard x8, Iridium Bar x1 | MINE_LOWEST_LEVEL_REACHED 40 | Shop + Drop |
| Ethereal Boots | Epic | Solar Essence x10, Void Essence x10, Iridium Bar x3 | MINE_LOWEST_LEVEL_REACHED 80 | Shop + Drop |
| Ethereal Armor | Epic | Solar Essence x8, Void Essence x8, Radioactive Bar x2 | MINE_LOWEST_LEVEL_REACHED 80 | Shop + Drop |

Ingredientes escolhidos por tema/progressão, não por fórmula: o conjunto
Miner's usa metais iniciais (Cobre/Ferro/Carvão); Obsidian usa Cinder
Shard/Iridium Bar (tema vulcânico, ecoa o Drop já configurado em Hot
Head/Lava Crab); Ethereal usa Solar/Void Essence (tema etéreo/fantasma,
ecoa o Drop em Carbon/Putrid Ghost) com um terceiro ingrediente distinto
entre Boots (Iridium Bar) e Armor (Radioactive Bar) para evitar receitas
idênticas dentro do mesmo conjunto.

### Equipamentos sem Crafting

Shadow Fang, Moon Dagger e Abyss Hammer permanecem só Shop+Drop — são os
itens mais "ligados a loot temático" do catálogo (Shadow Brute, Skeleton
Mage, Iridium Golem já configurados na Fase 7B), então crafting seria
redundante com a identidade já estabelecida. **Prismatic Blade** continua
sem Shop, sem Drop e agora também sem Crafting — nenhuma receita com
materiais caros foi criada só para preencher a tabela; ela permanece
reservada para uma futura fase de Quest/Reward.

### Shop + Drop + Crafting

Miner's Boots, Obsidian Boots, Obsidian Armor, Ethereal Boots e Ethereal
Armor têm as **três** formas de aquisição simultaneamente. Black Iron
Sword, Stonebreaker, Miner's Armor e Miner's Blade têm Shop + Crafting
(sem Drop). Nenhum método existente foi removido ao adicionar Crafting.

### Testes

`CraftingPipelineTests.cs` (novo, 21 testes): validação de definição
(ingredientes vazios, quantidade zero, itemId em branco, itemId duplicado,
unlockCondition em branco, ausência de Crafting permitida), catálogo (9 de
13 configurados, Prismatic Blade sem nenhuma aquisição, pelo menos um item
com Shop+Crafting sem Drop e pelo menos um com Shop+Drop+Crafting),
pipeline (`CraftingRecipeInjector.BuildRecipeString` produz o formato real
`ingredientes/Home/outputQualificado 1/false/default/` para arma/bota/
armadura, saída sempre quantidade 1, rejeita equipamento sem dados de
crafting), compatibilidade (`ApplyTo` preserva receita vanilla existente,
isola colisão sem bloquear as demais), unlock (`BuildTriggerAction` usa
`Trigger=DayStarted` e `Action=MarkCraftingRecipeKnown Current <id>`,
condição nula quando não configurada, `ApplyTo` sobre `Data/TriggerActions`
preserva entradas vanilla e é idempotente em chamadas repetidas — simulando
reinvalidação do asset sem duplicar trigger).

### Validação manual pendente (a executar após esta fase)

Abrir o menu de crafting em diferentes estágios de progresso e confirmar
que as receitas Common aparecem desde o primeiro dia e as Rare/Epic só após
a condição de mina correspondente; craftar pelo menos uma arma, uma bota e
uma armadura e confirmar consumo de ingredientes e item produzido
corretos; equipar, salvar e recarregar para confirmar que a receita
continua conhecida; confirmar que Shop e Drop continuam funcionando sem
regressão; multiplayer/split-screen (receita conhecida por jogador
diferente) não testado manualmente.

Próximo passo sugerido logo após a Fase 7C: Quest/Reward para a Prismatic
Blade (concluído como **Fase 7D**, ver abaixo).

## Fase 7D — aquisição via Quest / Reward (Prismatic Blade)

### Decisão técnica

`Data/SpecialOrders` (tipado em 1.6, `Dictionary<string, SpecialOrderData>`)
foi escolhido em vez do sistema legado `Data/Quests` — tem `Condition` (GSQ)
nativo, `Objectives`/`Rewards` estruturados, e é o mecanismo por-**equipe**
que o próprio jogo usa para conteúdo endgame (Qi Challenges, pedidos de
NPCs). O ponto crítico da fase — como conceder um item custom como
recompensa — foi resolvido por disassembly: `Reward Type="Object"`
constrói `StardewValley.Object` diretamente (não serve para uma arma), mas
o parser de correspondência (`LetterViewerMenu.HandleItemCommand`) resolve
o comando `%item id <QualifiedItemId> %%` via `ItemRegistry.Create` —
aceita qualquer item, incluindo os do Valley Armory. A cadeia final:
Special Order completa → `Reward Type="Mail"` → `Data/mail` com anexo
`%item id (W)...PrismaticBlade 1 %%`. Nenhum Harmony foi necessário.
Detalhes completos em `docs/signature-audit.md` (seção "Quest / Reward —
Fase 7D").

Ativação da quest reaproveita **exatamente** o mesmo padrão de
`Data/TriggerActions` da Fase 7C (`Trigger=DayStarted`, `Condition=GSQ`),
trocando apenas a ação por `AddSpecialOrder <questId>`. `FarmerTeam.
AddSpecialOrder` já é idempotente por design (confirmado por disassembly:
não faz nada se já ativa, e não faz nada se já concluída e não repetível)
— repetir a ação todo dia é seguro, sem necessidade de guarda extra no mod.

### Multiplayer — decisão explícita, não ambígua

Special Orders são **por-equipe** (`FarmerTeam.specialOrders`/
`completedSpecialOrders`), não por-jogador. `MailReward.Grant()` usa
`Game1.addMail(...)`, o broadcast vanilla de correspondência para **todos**
os jogadores conectados. Decisão: ao concluir a Prismatic Trial, **cada
membro da equipe recebe sua própria carta e sua própria Prismatic Blade**
— não apenas quem desferiu o golpe final. Isso evita duplicação (mail é
por-farmer, `mailReceived` não deixa reabrir a mesma recompensa duas vezes)
e trata a conquista como um marco do grupo, coerente com o próprio
comportamento vanilla de Special Orders.

### Arquitetura

```
EquipmentDefinition.Acquisition.Quest (QuestId, UnlockCondition?)
        ↓
QuestUnlockInjector.BuildTriggerAction     SpecialOrderInjector.BuildSpecialOrder     QuestMailInjector.BuildLetter
        ↓                                          ↓                                          ↓
Data/TriggerActions                        Data/SpecialOrders                        Data/mail
{Trigger=DayStarted,                       {Requester=Marlon,                        "<corpo traduzido> %item id
 Condition,                                 Objectives=[Slay 15                       (W)...PrismaticBlade 1 %%
 Action="AddSpecialOrder                    Iridium Golem],                          [#]<título traduzido>"
 romulot.ValleyArmory_PrismaticTrial"}      Rewards=[Mail →
                                             PrismaticTrialReward]}
```

`QuestBlueprints` (novo, `src/Acquisition/QuestBlueprints.cs`) centraliza o
conteúdo específico de cada quest (requester, objetivo, chaves de
tradução) por `QuestId` — não por identidade de equipamento — para que uma
futura segunda quest só exija um novo registro no dicionário, sem
qualquer branch por item.

### Quest

```
Nome (i18n): Prismatic Trial / Provação Prismática
ID: romulot.ValleyArmory_PrismaticTrial
Requester: Marlon
Duração: até o início da próxima estação (`QuestDuration.Month`)
Condição de desbloqueio: MINE_LOWEST_LEVEL_REACHED 120
Objetivo: derrotar 15 Golens de Irídio (Iridium Golem)
Spawn: 1 por andar/dia na mina normal 81–119, somente com a ordem ativa
Autoridade do spawn: host; aditivo, sem substituir monstros existentes
Reward: carta com Prismatic Blade anexada, para toda a equipe
Escopo multiplayer: Special Order por equipe; recompensa (mail) broadcast a todos os jogadores conectados
```

### Prismatic Blade

```
Shop: não
Drop: não
Crafting: não
Quest/Reward: sim
```

Confirmado e protegido por teste (`PrismaticBladeHasOnlyQuestAcquisition`).

### Testes

`QuestPipelineTests.cs` (novo, 12 testes): validação de definição
(questId obrigatório, unlockCondition em branco rejeitada, ausência de
Quest permitida), papel especial da Prismatic Blade (Shop/Drop/Crafting
ausentes e Quest presente, único equipamento com Quest no catálogo),
unlock (`BuildTriggerAction` usa `DayStarted`/`AddSpecialOrder`, `ApplyTo`
preserva entradas vanilla e é idempotente), Special Order (`BuildSpecialOrder`
produz objetivo Slay e reward Mail corretos, `ApplyTo` preserva entrada
vanilla existente), mail (`BuildLetter` anexa o Qualified Item ID correto
do equipamento vinculado, `ApplyTo` preserva carta vanilla existente).

### Validação manual pendente (a executar após esta fase)

Carregar um save sem a condição de mina 120 e confirmar que a quest não
aparece; avançar até a profundidade correspondente e confirmar que a
Special Order aparece disponível/ativa; percorrer os andares 81–119 e
confirmar um Iridium Golem por andar elegível/dia; derrotar 15 deles e
confirmar conclusão; verificar recebimento da carta com a Prismatic Blade
anexada; verificar tooltip/raridade Legendary/iluminação da espada
recebida; salvar e recarregar para confirmar que a quest continua
concluída e a recompensa não é concedida novamente; multiplayer (cada
farmhand recebendo sua própria carta/espada) não testado manualmente.

Próximo passo sugerido: balanceamento final e revisão de release — todas
as fontes de aquisição (Shop, Drop, Crafting, Quest/Reward) já têm
implementação real.

## Fase 8 — balanceamento e integração final

Revisão de todos os 13 equipamentos como um sistema único de progressão,
sem novo equipamento e sem nova mecânica. Auditoria completa em
`docs/balance-spec.md` (seção "Fase 8"), incluindo preços reais de venda
dos ingredientes de crafting extraídos de `Data/Objects`.

Resumo dos ajustes (todos em `assets/armory.json`, nenhum ID/pipeline/
arquitetura alterado):

- Preço de loja: `Miner's Blade` 900→1400g, `Shadow Fang` 1000→1300g
  (corrige armas Rare mais baratas que armas Common), `Obsidian Boots`
  900→1300g, `Obsidian Armor` 850→1250g (aproxima o preço de loja do
  custo real dos materiais).
- `critChance` de `Moon Dagger`: 0.12→0.10 (deixa de superar o teto do
  melhor punhal vanilla, `Iridium Needle`).
- Ingredientes de crafting: `Stonebreaker` ganhou Iron Bar x5;
  `Miner's Armor` teve Iron Bar 3→2 e Copper Bar 3→1; `Miner's Blade`
  teve Iron Bar 8→6 e Coal 15→13; `Obsidian Boots` teve Iridium Bar 2→1;
  `Obsidian Armor` teve Cinder Shard 8→4; `Ethereal Boots` teve Iridium
  Bar 3→1; `Ethereal Armor` trocou Radioactive Bar x2 por Iridium Bar x1.
- Condição de drop adicionada (antes ausente, inconsistente com o resto
  do catálogo): `Miner's Boots` (mina 10), `Obsidian Boots` (mina 40),
  `Obsidian Armor` (mina 40) — sem alterar nenhuma chance.

Nenhuma alteração em `Prismatic Blade`, `Black Iron Sword` (stats),
`Abyss Hammer`, `Ethereal Boots`/`Ethereal Armor` (preço de loja),
raridades, unlocks de Shop/Crafting/Quest, ou nos três assets de sprite.

### Testes

Um novo teste de regressão, `ShopPriceNeverDecreasesAsRarityIncreasesWithinTheSameEquipmentFamily`
(`AcquisitionPipelineTests.cs`), protege o Problema 1 acima (preço de loja
não pode cair ao subir de raridade dentro da mesma família de
equipamento: arma, bota ou armadura).

### Validação manual pendente (a executar após esta fase)

Early (mina 10): confirmar que `Black Iron Sword`/`Stonebreaker`/
`Miner's Boots`/`Miner's Armor` continuam compráveis e craftáveis, e que
o desconto do crafting é perceptível mas não gratuito. Mid (mina 40):
confirmar que os itens Rare (`Miner's Blade`, `Shadow Fang`,
`Obsidian Boots`, `Obsidian Armor`) agora custam mais que os Common
correspondentes. Late (mina 80): confirmar preços/drops dos Epic. Endgame
(mina 120): validar a Prismatic Trial e a recompensa como já descrito na
Fase 7D.

Próximo passo sugerido: nenhuma fase de conteúdo adicional planejada;
revisão de release (changelog, versão do manifest) fica como próximo
marco.
