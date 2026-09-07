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

### Tipos internos de armas confirmados

No assembly `Stardew Valley.dll` `1.6.15.24356`, os campos estáticos de
`StardewValley.Tools.MeleeWeapon` são:

```text
stabbingSword = 0
dagger        = 1
club          = 2
defenseSword  = 3
```

Portanto, `Dagger` é `1` e `Hammer/Club` é `2`, mas `Sword` possui pelo menos
dois comportamentos vanilla: espadas de estocada (`0`) e espadas defensivas
(`3`). A factory genérica não deve escolher um único valor de espada sem uma
decisão explícita por item.

### Botas

Confirmado em `Stardew Valley.dll`:

```csharp
public Boots(string itemId);
public Item BootsDataDefinition.CreateItem(ParsedItemData data);
protected Dictionary<string, string> BootsDataDefinition.GetDataSheet();
protected string[] BootsDataDefinition.GetRawData(string itemId);
```

Os dados extraídos de `Content/Data/Boots.xnb` na instalação alvo contêm 18
entradas vanilla e cada valor bruto possui **sete campos** separados por `/`.
A ordem real observada (confirmada carregando o asset real via
`ContentManager.Load<Dictionary<string,string>>("Data/Boots")`) é:

```text
0 Name
1 Description
2 Price (obrigatório no formato; preço efetivo é derivado pelo jogo)
3 Defense
4 Immunity
5 ColorIndex
6 DisplayName
```

### Extensão de 10 campos (Fase 6B — reauditoria)

A auditoria original da Fase 0 parou nos sete campos vanilla e concluiu que
`SpriteIndex`/`Texture` não são configuráveis por item. Isso estava
**incompleto**. Desmontando o IL de
`StardewValley.ItemTypeDefinitions.BootsDataDefinition.GetData`/`GetSpriteIndex`/`GetSourceRect`
e de `StardewValley.Objects.Boots.reloadData`/`GetBootsColorString` no assembly
`1.6.15.24356` (via `System.Reflection.Emit.OpCodes`, sem decompilador),
confirma-se que o array de campos aceita até **10 posições**, sendo as três
últimas uma extensão não documentada:

```text
7 Custom color-sheet texture name (opcional)
8 Custom SpriteIndex (opcional, inteiro)
9 Custom icon texture name (opcional)
```

Comportamento exato observado no IL:

- `GetSpriteIndex(id, fields)`: retorna `fields[8]` se presente (`GetInt` com
  default `-1`); senão tenta `int.Parse(id)`; senão `-1`. Isso explica por que
  os 18 itens vanilla (IDs numéricos legados) nunca precisam do campo 8: seu
  próprio ID já é o índice.
- `GetData(id)`: usa `fields[9]` como nome da textura do ícone, com fallback
  literal para `"Maps\springobjects"` (a sheet compartilhada onde os itens
  vanilla vivem, indexados pelo próprio ID numérico). Repare que o fallback já
  usa **barra invertida**, não `/`.
- `Boots.reloadData()`: `indexInTileSheet` (usado por `GetSourceRect`) vem de
  `ParsedItemData.SpriteIndex` (ou seja, do resultado de `GetSpriteIndex`
  acima); `Defense`/`Immunity`/`Price`/`ColorIndex` continuam vindo dos campos
  2-5, sem mudança.
- `Boots.GetBootsColorString()`: se `fields[7]` existir e não for vazio,
  retorna `"<fields[7]>:<indexInColorSheet>"` (textura de recolor customizada
  usada por `Farmer.changeShoeColor`); caso contrário retorna apenas o índice
  numérico, isto é, a paleta vanilla `Characters/Farmer/shoeColors.xnb`
  (confirmada como uma imagem-paleta pequena — 481 bytes descomprimidos no
  total incluindo cabeçalho —, não uma máscara de recolor do tamanho do
  spritesheet do farmer).
- `GetSourceRect`: `getSourceRectForStandardTileSheet(texture, spriteIndex, 16, 16)` — confirma células de **16×16px**, igual ao padrão já usado em `assets/weapons.png`.

**Restrição crítica descoberta**: o valor bruto inteiro é dividido por `/` pelo
próprio jogo (`raw.Split('/')`) antes de qualquer campo ser lido. Isso significa
que o campo 9 (nome da textura) **não pode conter `/`**, mesmo sendo
convencionalmente um caminho de asset (`Mods/Autor/Nome`). A solução, que é
exatamente o que o próprio fallback vanilla faz, é usar `\` no lugar de `/`
nesse campo — o SMAPI normaliza `/` e `\` como equivalentes na resolução de
nomes de asset, então isso não quebra `AssetRequestedEventArgs`/`LoadFromModFile`.

Consequência para a implementação: `BootDataFactory` gera os 10 campos
(mantendo os 7 vanilla intactos), usa o campo 8 para o `SpriteIndex` próprio e
o campo 9 (com `\` no lugar de `/`) para apontar para
`Mods/romulot.ValleyArmory/Boots`, evitando depender de IDs numéricos legados
ou de estender `Maps/springobjects.png`. O campo 7 (textura de recolor customizada)
foi deixado vazio nesta fase — ver limitação registrada em `docs/vertical-slice.md`.

A auditoria original que descrevia "dez campos" antes da Fase 2 estava, na
verdade, parcialmente correta — só não sabia dizer o que os 3 campos extras
significavam. Este documento substitui essa lacuna.

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

### Armaduras / wearables — Fase 6C (reauditoria)

Antes de implementar, foi confirmado no assembly `1.6.15.24356` que **não existe** um item
vanilla ou um slot chamado "Armor". O jogo representa roupa de corpo através de duas
classes de wearable, ambas materializadas em runtime pela mesma classe de item:

```csharp
public class StardewValley.Objects.Clothing  // classe de runtime tanto para shirt quanto para pants
{
    NetEnum<ClothesType> clothesType;         // enum: SHIRT, PANTS (confirmado via reflexão)
    NetInt indexInTileSheet;
    NetInt price;
    NetBool dyeable;
    NetColor clothesColor;
    NetBool isPrismatic;
}
```

Ao contrário de `Data/Boots` (formato legado de string), **`Data/Shirts` e `Data/Pants` já são
fortemente tipados** em 1.6, com um formato muito mais parecido ao de `Data/Weapons`:

```csharp
public class StardewValley.GameData.Shirts.ShirtData
{
    public string Name;
    public string DisplayName;
    public string Description;
    public int Price;
    public string Texture;      // asset explícito — igual a WeaponData.Texture, sem hack de campo extra
    public int SpriteIndex;
    public string DefaultColor;
    public bool CanBeDyed;
    public bool IsPrismatic;
    public bool HasSleeves;     // exclusivo de ShirtData — PantsData não tem
    public bool CanChooseDuringCharacterCustomization;
    public Dictionary<string, string> CustomFields;
}
```

`PantsData` tem os mesmos campos, exceto `HasSleeves`. Confirmado carregando o asset real
(`ContentManager.Load<Dictionary<string, ShirtData>>("Data/Shirts")`): 303 entradas vanilla,
todas com `Texture=null` (cai no default `Characters\Farmer\shirts`), `SpriteIndex` sequencial.
`Data/Pants` tem 18 entradas vanilla, mesmo padrão.

QualifiedItemId: confirmado via `ItemRegistry.type_shirt = "(S)"` e `ItemRegistry.type_pants = "(P)"`
(constantes públicas, junto com `type_boots="(B)"` e `type_weapon="(W)"` já usados).

**Formato do ícone (fonte de verdade: IL de `ShirtDataDefinition.GetSourceRect`, disassemblado
via `System.Reflection.Emit.OpCodes` sem decompilador):**

```text
columns = texture.Width / 2
x = (spriteIndex * 8) % columns
y = (spriteIndex * 8 / columns) * 32
rect = (x, y, 8, 8)
```

Ou seja, cada ícone é **8×8px**, a largura útil é só a METADE esquerda da textura (`columns`),
e cada "bloco" de sprite tem **32px de altura**. `PantsDataDefinition.GetSourceRect` usa uma
grade fixa de 192×688px com ícones de 16×16 — mais rígida e menos adequada a uma textura própria
pequena, o que reforça a escolha por Shirt nesta fase.

**Limitação importante, não resolvida nesta fase:** `FarmerRenderer.drawHairAndAccesories`
recalcula `shirtSourceRect` a cada desenho (não há um único ponto de "trocar roupa" que grave
o rect uma vez), e o método é grande o suficiente que não foi disassemblado por completo —
não temos confirmação de quais sub-regiões além do ícone (`y=0`) são amostradas quando a peça é
desenhada no personagem em movimento/direções diferentes. Mitigação adotada: `assets/armor.png`
replica o mesmo pixel art nas 4 sub-linhas de 8px de cada bloco de 32px (e também nas colunas
espelhadas da metade direita da textura, não usada quando `CanBeDyed=false`), então qualquer
sub-retângulo amostrado mostra arte válida e coerente, nunca lixo/transparência incorreta.
Isso ficou registrado como pendência de validação manual em `docs/vertical-slice.md`.

Também confirmado: `Clothing` **não sobrescreve** `drawTooltip` nem
`getExtraSpaceNeededForTooltipSpecialIcons` (ao contrário de `MeleeWeapon` e `Boots`), então o
patch de tooltip para armaduras precisou ser aplicado na declaração base em `Item`, não em
`Clothing` — ver `docs/vertical-slice.md` (seção Fase 6C) para o racional completo.

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

## Lojas / Adventurer's Guild — Fase 7A (reauditoria)

Auditoria feita via reflexão + carregamento real de `Data/Shops` no assembly instalado `1.6.15.24356` (mesma técnica de scratchpad das fases anteriores: `AssemblyLoadContext.Default.Resolving` + `LocalizedContentManager` headless, sem `GraphicsDevice`, funcional para assets puramente de dados).

### Localização real dos tipos

Ao contrário de `WeaponData`/`ShirtData`/`PantsData` (que vivem em `Stardew Valley.dll`), os tipos de loja vivem em um assembly **separado**: `StardewValley.GameData.dll` (mesma versão `1.6.15.24356`), namespace `StardewValley.GameData.Shops`:

- `ShopData` — dados de uma loja inteira (chave do dicionário `Data/Shops`).
- `ShopItemData` — uma entrada de item vendável dentro da loja.
- `ShopOwnerData` / `ShopDialogueData` / `ShopThemeData` / `LimitedStockMode` / `StackSizeVisibility` — suporte a NPC dono, diálogo, tema visual e modos de estoque.

`StardewValley.Internal.ShopBuilder` (em `Stardew Valley.dll`) é a classe que consome `ShopData`/`ShopItemData` para montar o estoque real (`GetShopStock`), resolver preço base (`GetBasePrice`) e donos atuais (`GetCurrentOwners`); não foi necessário reimplementar nenhuma dessas regras, apenas fornecer dados corretos.

### Asset e identificador real

`Data/Shops` → `Dictionary<string, ShopData>`, carregado e confirmado com **77 entradas** na instalação real. O identificador real da Adventurer's Guild é `AdventureShop` (não "AdventurersGuild", "Guild" ou qualquer variação do nome visual). Dono confirmado: `Marlon` (`ShopOwnerData.Id/Name = "Marlon"`, `Type = NamedNpc`).

Esse identificador foi centralizado em `ValleyArmory.Acquisition.ShopIdentifiers.AdventureGuild` — nenhuma string mágica solta pelo pipeline.

### Estrutura real de `ShopItemData` (campos relevantes)

```
Id                string   // chave estável da entrada dentro da lista (não precisa ser igual ao vanilla)
ItemId            string   // Qualified Item ID direto: "(W)0", "(B)507", "(O)529"
Price             int      // preço explícito, sem fórmula
AvailableStock    int      // -1 = ilimitado (confirmado: todas as entradas reais usam -1)
Condition         string   // Game State Query (GSQ) opcional
```

`Items` é um `List<ShopItemData>` (não dicionário) dentro de `ShopData` — edição aditiva correta é **append** à lista de uma entrada de dicionário já existente, não `TryAdd` num dicionário top-level como em `Data/Boots`/`Data/Shirts`. Isso exige um padrão de injeção ligeiramente diferente dos usados nas fases anteriores.

### Conditions reais confirmadas (Game State Query)

Inspecionando as 40 entradas reais de `AdventureShop.Items`, as condições usadas em produção pelo próprio jogo são todas GSQ nativas, por exemplo:

```
MINE_LOWEST_LEVEL_REACHED 10
MINE_LOWEST_LEVEL_REACHED 40
MINE_LOWEST_LEVEL_REACHED 80
PLAYER_HAS_MAIL Current galaxySword
PLAYER_HAS_CRAFTING_RECIPE Current Explosive Ammo
```

`MINE_LOWEST_LEVEL_REACHED` foi confirmado como query real e registrada (`GameStateQuery.QueryTypeLookup`, 115 queries registradas no total, incluindo `LOCATION_IS_MINES` e `MINE_LOWEST_LEVEL_REACHED`). Nenhuma sintaxe foi inventada — a curva de progressão do Valley Armory (10 / 40 / 80) reaproveita exatamente os patamares já usados pelo próprio `AdventureShop` vanilla para armas/botas de força comparável.

### Compatibilidade e isolamento de falhas

`ShopData.Items` já vem populado com 40 entradas vanilla no `AdventureShop`; a edição do Valley Armory é estritamente aditiva (`.Add` na lista existente), nunca substitui a lista nem a loja. `ShopItemData.Id` é único apenas por convenção dentro da lista (não há checagem de unicidade no motor); o injetor do Valley Armory usa o próprio ID namespaced (`romulot.ValleyArmory_...`) como `Id`, evitando colisão com rótulos vanilla como `"ElfBlade"` ou `"WorkBoots"`.

### Limitação observada

Não foi possível (nem necessário) auditar `SynchronizedShopStock` em profundidade — ela trata sincronização de estoque compartilhado em multiplayer para lojas com estoque limitado; como o Valley Armory usa `AvailableStock = -1` (ilimitado) para todos os itens, esse mecanismo não é exercitado nesta fase.

## Drops de monstro — Fase 7B (reauditoria)

Auditoria feita via reflexão + disassembly manual de IL (mesma técnica das fases anteriores) sobre `Stardew Valley.dll` `1.6.15.24356`.

### `Data/Monsters` continua no formato legado

Ao contrário de `Data/Shops`/`Data/Shirts`/`Data/Weapons` (fortemente tipados em 1.6), `Data/Monsters` **continua** sendo `Dictionary<string, string>` com um registro posicional separado por `/` (confirmado carregando o asset real: 51 monstros). O campo de drops fica embutido nesse registro como pares `itemId chance` (ex.: `766 .75 766 .05 153 .1 ...`), usando IDs de objeto simples — não há como declarar ali um Qualified Item ID `(W)`/`(B)`/`(S)`. Por isso, o Valley Armory **não edita `Data/Monsters`** para adicionar seus próprios drops.

### O ponto de extensão real: `Monster.getExtraDropItems()`

Localizado o mecanismo correto por disassembly: `StardewValley.Monsters.Monster` tem um método virtual `getExtraDropItems()` (retorna `List<Item>`, corpo base = `return new List<Item>();`) que é chamado exatamente uma vez por `GameLocation.monsterDrop(Monster, int, int, Farmer)`, junto com `Monster.ModifyMonsterLoot(Debris)`. Os itens retornados por `getExtraDropItems()` são convertidos em `Debris` reais pelo próprio `monsterDrop` (confirmado no IL: `Item.getOne()` → `set_Stack` → `new Debris(...)`), ou seja, **basta adicionar itens à lista retornada** — o spawn físico, posição e comportamento de coleta são 100% delegados ao pipeline vanilla, sem necessidade de `Game1.createItemDebris` manual.

Cinco subtipos (`Bat`, `BigSlime`, `Bug`, `Ghost`, `RockGolem`) sobrescrevem `getExtraDropItems()`, mas confirmado por IL que todos chamam `base.getExtraDropItems()` internamente antes de adicionar seus próprios itens especiais. Um único patch Harmony **postfix na implementação base** (`Monster.getExtraDropItems`) portanto se aplica a **todos os 51 monstros** de `Data/Monsters`, incluindo os 5 subtipos especiais — não foram necessários patches adicionais por subtipo.

### Autoridade em multiplayer (confirmada, não assumida)

Cadeia de chamadas real: `Monster.takeDamage(...)` → (dano aplicado) → `GameLocation.damageMonster(...)` → `GameLocation.onMonsterKilled(Farmer, Monster, Rectangle, bool)` → `GameLocation.monsterDrop(...)` → `Monster.getExtraDropItems()`. `damageMonster` é o método que processa o impacto do golpe do jogador e só é executado localmente pelo cliente cujo farmer desferiu o golpe (`onMonsterKilled` é chamado apenas nesse ponto da cadeia, não há chamada equivalente disparada a partir da sincronização de rede da vida do monstro). Ou seja: **a morte só é processada uma vez, pelo cliente do farmer atacante** — exatamente o mesmo modelo de autoridade que o próprio jogo já usa para os drops especiais de Ghost/Bat/Bug/RockGolem/BigSlime. Como o Valley Armory usa o mesmo ponto de extensão, herda essa garantia automaticamente, sem precisar de nenhuma checagem adicional tipo `Context.IsMainPlayer`.

### RNG confirmado

Disassembly de `Ghost.getExtraDropItems()` (um dos 5 overrides vanilla) mostra `Game1.random.NextDouble()` como fonte de aleatoriedade para decidir o drop especial — não uma seed própria nem `new Random()`. O Valley Armory usa exatamente a mesma fonte (`Game1.random`), replicando o padrão já usado pelo próprio jogo para este mecanismo.

### Identificador do monstro

`Monster.Name` (propriedade `string`) é a mesma string usada como chave em `Data/Monsters` e é o valor passado para `Stats.monsterKilled(name)` na contagem de mortes (confirmado no IL de `onMonsterKilled`) — é o identificador "interno" correto para o campo `SourceId`, não o nome visual/traduzido. Centralizado em `MonsterIdentifiers` (`src/Acquisition/MonsterIdentifiers.cs`).

### `GameStateQuery.CheckConditions` reaproveitada

Assinatura real confirmada: `GameStateQuery.CheckConditions(string query, GameLocation location, Farmer player, Item targetItem, Item inputItem, Random random, HashSet<string> ignoreQueryKeys)`. O Valley Armory reaproveita a mesma condição `MINE_LOWEST_LEVEL_REACHED` já usada na Fase 7A para a loja, chamando essa API diretamente (já que, ao contrário do Shop, o drop não é avaliado nativamente pelo motor — é o próprio código do mod que decide se o item cai).

## Crafting — Fase 7C (reauditoria)

Auditoria feita por reflexão + disassembly manual de IL sobre `Stardew Valley.dll` `1.6.15.24356`, com foco no ponto crítico: se o crafting vanilla aceita produzir diretamente um Weapon/Boots/Shirt custom.

### `Data/CraftingRecipes` continua no formato legado

Igual a `Data/Monsters`, `Data/CraftingRecipes` **não foi migrado** para tipo forte em 1.6: continua `Dictionary<string,string>` (confirmado carregando o asset real, 150 receitas). Formato real confirmado por disassembly de `CraftingRecipe..ctor(string,bool)` (não por memória): `"ingredienteId quantidade [...]/categoria/outputId quantidade/bigCraftable/unlockFlag/"`. O campo de output também aceita múltiplos pares `id quantidade` (mecânica de "escolha aleatória entre resultados", usada por poucas receitas vanilla); o Valley Armory usa sempre um único par.

### Ponto crítico resolvido: `ItemRegistry.Create` aceita Qualified Item ID no output

Disassembly de `CraftingRecipe.createItem()` confirma: o método obtém o `QualifiedItemId` via `GetItemData(false)` e chama `ItemRegistry.Create(qualifiedItemId, numberProducedPerCraft, 0, 0)` **diretamente com essa string**. `GetItemData` por sua vez, quando a receita não é `bigCraftable`, chama `ItemRegistry.GetDataOrErrorItem(id)` **sem qualificar/prefixar** o id armazenado — ou seja, se o campo de output já contém um Qualified Item ID completo (`(W)...`, `(B)...`, `(S)...`), a resolução funciona exatamente como em qualquer outro ponto do jogo que use `ItemRegistry`. **Não foi necessário Harmony para o output** — vanilla já suporta produzir qualquer item registrado via `ItemRegistry`, incluindo os equipamentos custom do Valley Armory.

### Unlock de receita: `Farmer.craftingRecipes` é por-jogador

`Farmer.craftingRecipes` é um `NetStringDictionary<int, NetInt>` — estado de rede **por Farmer**, não do save inteiro. Cada farmhand pode conhecer receitas diferentes. `Farmer.LearnDefaultRecipes()` (chamado na criação do personagem) lê o índice 4 do registro bruto de `Data/CraftingRecipes` e, se for literalmente a string `"default"`, adiciona a receita ao dicionário do farmer — mas isso só executa uma vez, na criação do personagem, não ajuda saves já existentes.

### Mecanismo de unlock declarativo usado: `Data/TriggerActions` + `MarkCraftingRecipeKnown`

Confirmado tipo real `StardewValley.GameData.TriggerActionData` (asset `Data/TriggerActions`, uma **List**, não um Dictionary) com campos `Id`, `Trigger`, `Condition` (GSQ), `SkipPermanentlyCondition`, `HostOnly` (bool), `Action`. Existe uma ação padrão registrada `MarkCraftingRecipeKnown` (`TriggerActionManager.DefaultActions.MarkCraftingRecipeKnown`), com assinatura de argumentos `[ação, PlayerActionTarget, recipeKey, learned?]`, confirmada via disassembly chamando `Farmer.team.RequestSetSimpleFlag` (mecanismo de rede já existente, correto para multiplayer). `PlayerActionTarget` é um enum real com membros `Current | Host | All` (confirmado por reflexão). As 31 entradas vanilla de `Data/TriggerActions` inspecionadas têm todas `HostOnly=False` — ou seja, cada cliente avalia o trigger para o seu próprio jogador local, exatamente o comportamento desejado ("cada player pode conhecer receitas diferentes"). O Valley Armory usa `Trigger="DayStarted"` (valor real confirmado em uso vanilla) + `Condition` opcional (reaproveitando `MINE_LOWEST_LEVEL_REACHED`, já auditado na Fase 7A) + `Action="MarkCraftingRecipeKnown Current <recipeId>"`. Nenhum Harmony, nenhum `UpdateTicked`, nenhuma checagem manual de `IsMainPlayer` foi necessária — o mecanismo já resolve autoridade e idempotência (repetir a ação com a receita já conhecida é inofensivo).

### Ingredientes

Ingredientes usam IDs de objeto simples (sem qualificador), exatamente como no vanilla (`334`=Copper Bar, `335`=Iron Bar, `382`=Coal, `343`=Stone, `337`=Iridium Bar, `848`=Cinder Shard, `768`=Solar Essence, `769`=Void Essence, `910`=Radioactive Bar — todos confirmados via `Data/Objects` real, não memória).

## Limitações

- A auditoria foi estática; o jogo não foi iniciado.
- Nenhum tooltip ou efeito de luz foi observado visualmente.
- Nenhum cenário multiplayer ou split-screen foi executado.
- A ordem entre patches Harmony de outros mods não pode ser garantida sem uma matriz concreta de compatibilidade.
- Os métodos e campos confirmados são APIs públicas em termos de visibilidade CLR, mas pertencem ao código do jogo, não a uma garantia de estabilidade do SMAPI.
- A autoridade de multiplayer para `getExtraDropItems()` foi confirmada via análise estática da cadeia de chamadas (IL), não observada em uma sessão multiplayer real.
- A autoridade por-jogador de `Data/TriggerActions`/`MarkCraftingRecipeKnown` foi confirmada por `HostOnly=False` nas entradas vanilla e pela assinatura de `RequestSetSimpleFlag`, não observada em uma sessão multiplayer real.
