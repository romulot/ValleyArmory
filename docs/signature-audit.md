# Valley Armory — auditoria de assinaturas

## Escopo

Auditoria bloqueante da Fase 0 para o Valley Armory. Este documento registra apenas fatos observados nos assemblies instalados e as consequências para o plano. Nenhum código do mod foi criado nesta fase.

Identidade fixada:

- `UniqueID`: `romulot.ValleyArmory`
- versão inicial: `0.1.0`
- versão alvo do jogo: Stardew Valley `1.6.15`
- versão alvo do SMAPI: `4.5.2`

## Ambiente observado

Instalação Steam examinada:

```text
/home/romulo/.steam/steam/steamapps/common/Stardew Valley
```

| Assembly | Versão observada | SHA-256 |
|---|---|---|
| `Stardew Valley.dll` | `1.6.15.24356` | `f3e97f01d3fd2b1e6094fc8d2b59950aa6cb9d6cd1bf1b39d72d58edda8aad12` |
| `StardewValley.GameData.dll` | carregado junto ao jogo | `352e3b9189cdee588f88b1f956db368c56caf89e45258b0f75377f2225dcf311` |
| `StardewModdingAPI.dll` | `4.5.2.0` | `d0d40338afce227c7c69af555d7abd4a7077a4c9cf5fe2c2a94ae685cc79d88a` |
| `smapi-internal/0Harmony.dll` | `2.2.2.0` | `4c5497325157c3855c45119098f34eff6009613db0e2e9c245a7ff46bc45da89` |

As DLLs devem ser referenciadas pela instalação local através de `Pathoschild.Stardew.ModBuildConfig`. Elas não serão copiadas para o repositório nem incluídas no pacote do mod.

## IDs permanentes

Os IDs abaixo ficam congelados antes do primeiro save de teste. O catálogo guardará o ID não qualificado; o tipo vanilla compõe o `QualifiedItemId`.

| Item | ID não qualificado permanente | QualifiedItemId |
|---|---|---|
| Miner's Blade | `romulot.ValleyArmory_MinersBlade` | `(W)romulot.ValleyArmory_MinersBlade` |
| Black Iron Sword | `romulot.ValleyArmory_BlackIronSword` | `(W)romulot.ValleyArmory_BlackIronSword` |
| Prismatic Blade | `romulot.ValleyArmory_PrismaticBlade` | `(W)romulot.ValleyArmory_PrismaticBlade` |
| Shadow Fang | `romulot.ValleyArmory_ShadowFang` | `(W)romulot.ValleyArmory_ShadowFang` |
| Moon Dagger | `romulot.ValleyArmory_MoonDagger` | `(W)romulot.ValleyArmory_MoonDagger` |
| Stonebreaker | `romulot.ValleyArmory_Stonebreaker` | `(W)romulot.ValleyArmory_Stonebreaker` |
| Abyss Hammer | `romulot.ValleyArmory_AbyssHammer` | `(W)romulot.ValleyArmory_AbyssHammer` |
| Miner's Boots | `romulot.ValleyArmory_MinersBoots` | `(B)romulot.ValleyArmory_MinersBoots` |
| Obsidian Boots | `romulot.ValleyArmory_ObsidianBoots` | `(B)romulot.ValleyArmory_ObsidianBoots` |
| Ethereal Boots | `romulot.ValleyArmory_EtherealBoots` | `(B)romulot.ValleyArmory_EtherealBoots` |

Renomear display name ou tradução não altera esses IDs. Se um ID precisar ser substituído depois de testes com saves, será necessária uma migração explícita; não será feita renomeação silenciosa.

## Registro e criação de itens

### Armas

Confirmado em `StardewValley.GameData.dll`:

```csharp
public class StardewValley.GameData.Weapons.WeaponData
{
    public string Name;
    public string DisplayName;
    public string Description;
    public int Type;
    public string Texture;
    public int SpriteIndex;
    public int MinDamage;
    public int MaxDamage;
    public float Knockback;
    public int Speed;
    public int Precision;
    public int Defense;
    public int AreaOfEffect;
    public float CritChance;
    public float CritMultiplier;
    public bool CanBeLostOnDeath;
    public int MineBaseLevel;
    public int MineMinLevel;
    public Dictionary<string, string> CustomFields;
    public List<WeaponProjectile> Projectiles;
}
```

Também foram confirmados:

```csharp
public MeleeWeapon(string itemId);
public Item WeaponDataDefinition.CreateItem(ParsedItemData data);
```

Consequência: `Data/Weapons` pode carregar integralmente sprite e atributos vanilla. Raridade continuará sendo metadado do catálogo; não modificará stats automaticamente.

### Botas

Confirmado em `Stardew Valley.dll`:

```csharp
public Boots(string itemId);
public Item BootsDataDefinition.CreateItem(ParsedItemData data);
protected Dictionary<string, string> BootsDataDefinition.GetDataSheet();
protected string[] BootsDataDefinition.GetRawData(string itemId);
```

O binário confirma que botas ainda são interpretadas como `Dictionary<string, string>` e separadas em campos. A ordem de dez campos adotada pelo plano permanece:

```text
0 Name
1 Description
2 Price (obrigatório no formato; preço efetivo é derivado pelo jogo)
3 Defense
4 Immunity
5 ColorIndex
6 DisplayName
7 ColorTexture
8 SpriteIndex
9 Texture
```

Consequência: o gerador terá teste exato de dez posições e rejeitará `/` em valores não escapáveis. O `Price` configurado para botas não deve ser apresentado como preço efetivo sem confirmar a fórmula no teste integrado.

### ItemRegistry

Assinaturas confirmadas:

```csharp
public static Item Create(string itemId, int amount, int quality, bool allowNull);
public static TItem Create<TItem>(string itemId, int amount, int quality, bool allowNull);
public static ParsedItemData GetData(string itemId);
public static ParsedItemData GetDataOrErrorItem(string itemId);
```

Constantes públicas `ItemRegistry.type_weapon` e `ItemRegistry.type_boots` também existem.

Uso planejado para a prova vertical:

```csharp
ItemRegistry.Create("(W)romulot.ValleyArmory_MinersBlade", 1, 0, false)
```

O comando de desenvolvimento que entrega um item deve passar pelo mesmo caminho, sem construir `MeleeWeapon` ou `Boots` manualmente.

## Tooltip

Não foi encontrado evento público do SMAPI que represente semanticamente a medição e o desenho de um tooltip de item. Foram confirmados no jogo os seguintes pontos:

```csharp
public static void IClickableMenu.drawToolTip(
    SpriteBatch b,
    string hoverText,
    string hoverTitle,
    Item hoveredItem,
    bool heldItem,
    int healAmountToDisplay,
    int currencySymbol,
    string extraItemToShowIndex,
    int extraItemToShowAmount,
    CraftingRecipe craftingIngredients,
    int moneyAmountToShowAtBottom,
    IList<Item> additionalCraftMaterials
);

public static void IClickableMenu.drawHoverText(
    SpriteBatch b,
    StringBuilder text,
    SpriteFont font,
    int xOffset,
    int yOffset,
    int moneyAmountToDisplayAtBottom,
    string boldTitleText,
    int healAmountToDisplay,
    string[] buffIconsToDisplay,
    Item hoveredItem,
    int currencySymbol,
    string extraItemToShowIndex,
    int extraItemToShowAmount,
    int overrideX,
    int overrideY,
    float alpha,
    CraftingRecipe craftingIngredients,
    IList<Item> additionalCraftMaterials,
    Texture2D boxTexture,
    Rectangle? boxSourceRect,
    Color? textColor,
    Color? textShadowColor,
    float boxScale,
    int boxWidthOverride,
    int boxHeightOverride
);
```

O overload de `drawHoverText` com `string` apenas encaminha para o overload com `StringBuilder`.

Pontos virtuais confirmados para conteúdo e medição:

```csharp
public virtual void Item.drawTooltip(
    SpriteBatch spriteBatch,
    ref int x,
    ref int y,
    SpriteFont font,
    float alpha,
    StringBuilder overrideText
);

public virtual Point Item.getExtraSpaceNeededForTooltipSpecialIcons(
    SpriteFont font,
    int minWidth,
    int horizontalBuffer,
    int startingHeight,
    StringBuilder descriptionText,
    string boldTitleText,
    int moneyAmountToDisplayAtBottom
);
```

`MeleeWeapon` e `Boots` sobrescrevem os dois métodos. O IL de `IClickableMenu.drawHoverText` chama `hoveredItem.getExtraSpaceNeededForTooltipSpecialIcons(...)` durante a medição e `hoveredItem.drawTooltip(...)` durante o desenho.

O título é desenhado dentro do overload `StringBuilder` por três chamadas a `SpriteBatch.DrawString`: duas para sombra e uma para a cor principal. A chamada principal lê o parâmetro `Color? textColor`. Alterar esse parâmetro globalmente pode também afetar outros textos, portanto não atende sozinho ao requisito “somente o nome azul”.

### Decisão revisada

- Linha textual de raridade: patch Harmony isolado nos overrides de `MeleeWeapon.drawTooltip` e `MeleeWeapon.getExtraSpaceNeededForTooltipSpecialIcons`, limitado a qualified IDs do catálogo.
- Cor somente do título: transpiler mínimo no overload `IClickableMenu.drawHoverText(StringBuilder, ...)`, substituindo apenas a obtenção da cor imediatamente antes da chamada principal que desenha `boldTitleText`.
- O transpiler deve validar uma assinatura e um padrão de IL únicos. Zero ou múltiplas correspondências desativam toda a decoração de tooltip e registram um único `Warning`.
- Os patches de medição, linha e título serão habilitados atomicamente. Não haverá fallback que desenhe por coordenadas estimadas em `RenderedActiveMenu`.
- Compatibilidade com outros patches será testada no jogo; `HarmonyBefore`/`HarmonyAfter` só será usado para integrações conhecidas.

Apesar de tecnicamente possível no binário auditado, colorir apenas o nome é o ponto mais sensível a atualizações. Esta dependência interna deve permanecer documentada e coberta por teste de smoke de assinatura.

## Iluminação

`StardewValley.LightSource` usa ID textual em 1.6.15, não identificador inteiro:

```csharp
public LightSource();

public LightSource(
    string id,
    int textureIndex,
    Vector2 position,
    float radius,
    LightSource.LightContext lightContext,
    long playerID,
    string onlyLocation
);

public LightSource(
    string id,
    int textureIndex,
    Vector2 position,
    float radius,
    Color color,
    LightSource.LightContext lightContext,
    long playerID,
    string onlyLocation
);

public string Id { get; set; }
public long PlayerID { get; set; }
public NetVector2 position;
public NetColor color;
public NetFloat radius;
public NetInt fadeOut;
```

Operações públicas confirmadas em `GameLocation`:

```csharp
public LightSource getLightSource(string identifier);
public bool hasLightSource(string identifier);
public void removeLightSource(string identifier);
public void repositionLightSource(string identifier, Vector2 position);
public NetStringDictionary<LightSource, NetRef<LightSource>> sharedLights;
```

O IL confirma que consulta, remoção e reposicionamento operam sobre `GameLocation.sharedLights`. A inserção não possui wrapper público específico e deve ser feita por uma adição controlada à coleção pública:

```csharp
location.sharedLights.Add(lightId, lightSource);
```

`Game1.currentLightSources` também existe, mas é um cache local alimentado pelos eventos de mudança de `sharedLights`; ele não será usado como fonte de verdade nem modificado diretamente.

### IDs de luz

Formato reservado:

```text
romulot.ValleyArmory/weapon-light/<UniqueMultiplayerID>
```

Isso remove a necessidade do antigo hash inteiro e torna a propriedade auditável. Cada controlador mantém a localização dona da luz, além do ID.

### Ciclo de vida confirmado como implementável

- criar somente se `hasLightSource(id)` for falso;
- acompanhar movimento com `repositionLightSource(id, position)`;
- atualizar cor/raio na instância existente apenas quando a definição efetiva mudar;
- remover na localização dona com `removeLightSource(id)`;
- remover da origem antes de inserir no destino em `Player.Warped`;
- limpar em `DayEnding`, `ReturnedToTitle` e desconexão;
- manter atualização idempotente em `UpdateTicked`;
- usar `PerScreen<T>` para estado local de split-screen.

Eventos confirmados em SMAPI 4.5.2: `Player.Warped`, `GameLoop.UpdateTicked`, `DayStarted`, `DayEnding`, `SaveLoaded` e `ReturnedToTitle`.

### Ressalva multiplayer

`sharedLights` é uma coleção de rede. A API é utilizável, mas a Fase 0 não executou host/farmhand. A prova vertical deve confirmar:

- se cada cliente pode criar a luz de seu farmer sem disputa de autoridade;
- como a coleção converge quando dois peers possuem o mod;
- se criar a mesma luz cosmética em cada cliente causa duplicação visual;
- comportamento de split-screen;
- remoção ao desconectar.

Até essa validação, não se deve afirmar que “cada cliente calcula todas as luzes localmente”. Se a coleção já sincronizar corretamente a luz criada pelo dono, o desenho remoto deve reutilizar esse estado e não recriá-lo. Esta é uma mudança conservadora no plano original.

## Ferramentas de desenvolvimento planejadas

Serão isoladas em `src/DeveloperTools/` e registradas como comandos SMAPI. Não participarão de catálogo, identidade, tooltip ou iluminação em runtime normal.

| Comando planejado | Finalidade |
|---|---|
| `va_list` | listar ID curto, qualified ID, tipo, raridade e validade das definições |
| `va_give <id> [quantidade]` | resolver ID curto ou qualified ID exato e entregar via `ItemRegistry.Create` |
| `va_debug [on|off|status]` | habilitar diagnósticos transitórios de identidade, raridade e estado da luz |

Regras:

- `va_give` não aceita nome traduzido;
- IDs ambíguos ou desconhecidos falham sem criar error item;
- debug não será persistido no save;
- nenhum log será emitido a cada frame;
- transições serão registradas somente quando arma, raridade, localização ou estado da luz mudar.

## Especificação de balanceamento

Antes de implementar aquisição, `docs/balance-spec.md` deverá registrar, para cada um dos dez itens:

| Campo obrigatório | Aplicação |
|---|---|
| Item | todos |
| Tipo | todos |
| Raridade | todos |
| Dano mínimo/máximo | armas |
| Velocidade | armas |
| Defesa | armas e botas |
| Chance e multiplicador crítico | armas |
| Knockback | armas |
| Imunidade | botas |
| Preço | todos, distinguindo valor configurado e preço efetivo do jogo |
| Referência vanilla | todos |
| Justificativa do sidegrade | todos |

Raridade jamais aplicará multiplicadores automáticos. Todo atributo efetivo será explícito na definição do item.

## Plano revisado: vertical slice

1. **Fase 0 — auditoria de assinaturas:** este documento. Bloqueante e concluída estaticamente; falta validação em runtime, que pertence à prova vertical.
2. **Fase 1 — fundação:** projeto `0.1.0`, manifesto `romulot.ValleyArmory`, build reproduzível, harness de testes e comandos vazios/isolados.
3. **Fase 2 — catálogo e balanceamento:** schema, IDs permanentes, raridades e `docs/balance-spec.md`; inicialmente apenas Miner's Blade recebe valores executáveis, embora todos os IDs fiquem reservados.
4. **Fase 3 — vertical slice Miner's Blade:** `armory.json` → `Data/Weapons` → criação por `(W)romulot.ValleyArmory_MinersBlade` → sprite → stats → Rare → título azul → linha textual → luz azul fraca → limpeza.
5. **Fase 4 — validação da prova vertical:** jogo 1.6.15/SMAPI 4.5.2, menus/escala, troca/desequipar/warp/dormir/título e multiplayer. Nenhum dos outros nove itens avança antes da aprovação deste portão.
6. **Fase 5 — expansão do catálogo:** seis armas e três botas restantes, usando o pipeline já validado.
7. **Fase 6 — aquisição:** somente após a especificação de balanceamento aprovada.
8. **Fase 7 — documentação, matriz completa e release.**

## Critério de aceite da Fase 0

- [x] assemblies reais do Stardew Valley 1.6.15 e SMAPI 4.5.2 localizados;
- [x] versões e hashes registrados;
- [x] `WeaponData`, criação por `ItemRegistry` e tipo de dados de botas confirmados;
- [x] assinaturas de tooltip e pontos de medição/desenho confirmados;
- [x] construtores de `LightSource` e operações de `GameLocation` confirmados;
- [x] dependências em comportamento interno documentadas;
- [x] IDs permanentes, ferramentas de desenvolvimento, especificação de balanceamento e sequência vertical incorporados ao plano;
- [ ] comportamento renderizado, multiplayer e split-screen — deliberadamente reservado para a validação da vertical slice.

## Limitações

- A auditoria foi estática; o jogo não foi iniciado.
- Nenhum tooltip ou efeito de luz foi observado visualmente.
- Nenhum cenário multiplayer ou split-screen foi executado.
- A ordem entre patches Harmony de outros mods não pode ser garantida sem uma matriz concreta de compatibilidade.
- Os métodos e campos confirmados são APIs públicas em termos de visibilidade CLR, mas pertencem ao código do jogo, não a uma garantia de estabilidade do SMAPI.
