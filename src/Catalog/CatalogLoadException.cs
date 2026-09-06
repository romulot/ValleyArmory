namespace ValleyArmory.Catalog;

internal sealed class CatalogLoadException : Exception
{
    public CatalogLoadException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
