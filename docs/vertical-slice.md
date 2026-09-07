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

Próximo passo sugerido: **Fase 6C** (a definir — capacete/peitoral/pernas,
aquisição/drops, ou outro escopo, fora do que foi encerrado nesta fase).
