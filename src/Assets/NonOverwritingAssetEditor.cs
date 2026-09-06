namespace ValleyArmory.Assets;

internal static class NonOverwritingAssetEditor
{
    public static bool TryAdd<TValue>(IDictionary<string, TValue> data, string id, TValue value)
    {
        if (data.ContainsKey(id))
        {
            return false;
        }

        data.Add(id, value);
        return true;
    }
}
