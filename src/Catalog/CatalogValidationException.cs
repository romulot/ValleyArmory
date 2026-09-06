namespace ValleyArmory.Catalog;

internal sealed class CatalogValidationException : Exception
{
    public CatalogValidationException(IReadOnlyList<string> errors)
        : base($"The armory catalog is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}")
    {
        this.Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
