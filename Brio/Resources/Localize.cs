using System.Collections.Generic;
using System.Text.Json;

namespace Brio.Resources;

internal static class Localize
{
    private static readonly Dictionary<string, string> _stringDb = [];

    public static string Get(string key, string? defaultValue = null)
    {
        if(_stringDb.TryGetValue(key, out var value))
            return value;
        return defaultValue ?? key;
    }

    public static string? GetNullable(string key)
    {
        if(_stringDb.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public static void Load(ResourceProvider provider)
    {
        _stringDb.Clear();
        var raw = provider.GetRawResourceString("Language.en.json");
        using JsonDocument doc = JsonDocument.Parse(raw);
        FlattenRecursive(doc.RootElement, "");
    }

    private static void FlattenRecursive(JsonElement element, string currentKey)
    {
        switch(element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach(var property in element.EnumerateObject())
                {
                    FlattenRecursive(property.Value, $"{currentKey}{property.Name}.");
                }
                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach(var item in element.EnumerateArray())
                {
                    FlattenRecursive(item, $"{currentKey}{index}.");
                    index++;
                }
                break;
            default:
                _stringDb.Add(currentKey.TrimEnd('.'), element.ToString());
                break;
        }
    }
}
