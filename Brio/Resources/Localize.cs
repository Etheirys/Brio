using System.Collections.Generic;
using System.Text.Json;

namespace Brio.Resources;

public static class Localize
{
    private const string DefaultLanguage = "en";

    private static readonly Dictionary<string, string> _stringDb = [];

    public static string CurrentLanguage { get; private set; } = DefaultLanguage;

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

    public static void Load(ResourceProvider provider, string? clientLanguage = null)
    {
        _stringDb.Clear();

        LoadLanguage(provider, DefaultLanguage);

        CurrentLanguage = ResolveLanguage(clientLanguage);
        if(CurrentLanguage != DefaultLanguage)
            LoadLanguage(provider, CurrentLanguage, required: false);
    }

    private static string ResolveLanguage(string? clientLanguage)
    {
        return clientLanguage switch
        {
            "ChineseSimplified" => "zh-CN",
            _ => DefaultLanguage,
        };
    }

    private static void LoadLanguage(ResourceProvider provider, string language, bool required = true)
    {
        string raw;
        try
        {
            raw = provider.GetRawResourceString($"Language.{language}.json");
        }
        catch when(!required)
        {
            return;
        }

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
                _stringDb[currentKey.TrimEnd('.')] = element.ToString();
                break;
        }
    }
}
