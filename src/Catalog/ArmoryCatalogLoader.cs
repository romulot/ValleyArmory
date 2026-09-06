using System.Text.Json;
using System.Text.Json.Serialization;

namespace ValleyArmory.Catalog;

internal sealed class ArmoryCatalogLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly ArmoryCatalogValidator validator;

    public ArmoryCatalogLoader(ArmoryCatalogValidator validator)
    {
        this.validator = validator;
    }

    public ArmoryCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new CatalogLoadException($"Armory catalog file not found: '{path}'.");
        }

        ArmoryCatalog catalog;
        try
        {
            string json = File.ReadAllText(path);
            catalog = JsonSerializer.Deserialize<ArmoryCatalog>(json, SerializerOptions)
                ?? throw new CatalogLoadException($"Armory catalog '{path}' contains no data.");
        }
        catch (JsonException exception)
        {
            throw new CatalogLoadException(
                $"Armory catalog '{path}' contains invalid JSON at line {exception.LineNumber}, byte {exception.BytePositionInLine}: {exception.Message}",
                exception
            );
        }
        catch (IOException exception)
        {
            throw new CatalogLoadException($"Armory catalog '{path}' could not be read: {exception.Message}", exception);
        }

        this.validator.ValidateAndThrow(catalog);
        return catalog;
    }
}
