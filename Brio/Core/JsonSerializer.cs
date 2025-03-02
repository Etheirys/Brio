using Brio.Files.Converters;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.Core;

public static class JsonSerializer
{
    private readonly static JsonSerializerOptions _serializeOptions;
    private readonly static JsonSerializerOptions _legacySerializeOptions;

    static JsonSerializer()
    {
        _serializeOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        _serializeOptions.Converters.Add(new JsonStringEnumConverter());
        _serializeOptions.Converters.Add(new Vector2Converter());
        _serializeOptions.Converters.Add(new Vector3Converter());
        _serializeOptions.Converters.Add(new Vector4Converter());
        _serializeOptions.Converters.Add(new QuaternionConverter());

        _legacySerializeOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        _legacySerializeOptions.Converters.Add(new JsonStringEnumConverter());
        _legacySerializeOptions.Converters.Add(new Vector2Converter());
        _legacySerializeOptions.Converters.Add(new Vector3Converter());
        _legacySerializeOptions.Converters.Add(new Vector4Converter());
        _legacySerializeOptions.Converters.Add(new QuaternionConverter());
        _legacySerializeOptions.Converters.Add(new LegacyGlassesSaveConverter());
    }

    public static T Deserialize<T>(string json)
    {
        T? obj;
        try
        {
            obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, _serializeOptions);
        }
        catch
        {
            obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, _legacySerializeOptions);
        }

        if(obj == null)
            throw new Exception($"Failed to deserialize");

        return (T)obj;
    }

    public static string Serialize(object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, _serializeOptions);
    }
}
