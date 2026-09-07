using StardewModdingAPI;
using StardewModdingAPI.Events;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;

namespace ValleyArmory.Acquisition;

/// <summary>Adds Valley Armory crafting recipes to Data/CraftingRecipes additively, without replacing existing entries.</summary>
internal sealed class CraftingRecipeInjector
{
    private readonly IReadOnlyList<EquipmentDefinition> craftableEquipment;
    private readonly IMonitor monitor;
    private readonly HashSet<string> collisionLogged = new(StringComparer.Ordinal);

    public CraftingRecipeInjector(CatalogIndex catalog, IMonitor monitor)
    {
        this.craftableEquipment = catalog.GetAllEquipment()
            .Where(item => item.Acquisition?.Crafting is not null)
            .ToArray();
        this.monitor = monitor;
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(this.EditCraftingRecipes, AssetEditPriority.Default);
        }
    }

    private void EditCraftingRecipes(IAssetData asset)
    {
        this.ApplyTo(asset.AsDictionary<string, string>().Data);
    }

    internal void ApplyTo(IDictionary<string, string> recipes)
    {
        foreach (EquipmentDefinition definition in this.craftableEquipment)
        {
            try
            {
                string recipe = BuildRecipeString(definition);
                if (!NonOverwritingAssetEditor.TryAdd(recipes, definition.Id, recipe))
                {
                    if (this.collisionLogged.Add(definition.Id))
                    {
                        this.monitor.Log(
                            $"Skipped crafting recipe injection because Data/CraftingRecipes already contains ID '{definition.Id}'. Existing entry was preserved.",
                            LogLevel.Warn
                        );
                    }

                    continue;
                }

                this.monitor.Log($"Injected crafting recipe '{definition.Id}'.", LogLevel.Trace);
            }
            catch (Exception exception)
            {
                this.monitor.Log($"Invalid crafting acquisition for '{definition.Id}': {exception.Message}", LogLevel.Error);
            }
        }
    }

    /// <summary>Builds the raw Data/CraftingRecipes value: "ingredientId amount [...]/category/outputQualifiedId amount/bigCraftable/unlock/".</summary>
    internal static string BuildRecipeString(EquipmentDefinition definition)
    {
        CraftingAcquisition crafting = definition.Acquisition?.Crafting
            ?? throw new InvalidOperationException($"Equipment '{definition.Id}' has no crafting acquisition data.");

        if (crafting.Ingredients.Count == 0)
        {
            throw new InvalidOperationException($"Equipment '{definition.Id}' has no crafting ingredients.");
        }

        string ingredients = string.Join(" ", crafting.Ingredients.Select(ingredient => $"{ingredient.ItemId} {ingredient.Quantity}"));
        string outputId = EquipmentIdentity.GetQualifiedItemId(definition);

        return $"{ingredients}/Home/{outputId} 1/false/default/";
    }
}
