using Brio.Files.Converters;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.Core;

public static class JsonSerializer
{
    private readonly static JsonSerializerOptions _serializeOptions;

    static JsonSerializer()
    {
        _serializeOptions = new();
        _serializeOptions.WriteIndented = true;

        _serializeOptions.Converters.Add(new JsonStringEnumConverter());
        _serializeOptions.Converters.Add(new Vector2Converter());
        _serializeOptions.Converters.Add(new Vector3Converter());
        _serializeOptions.Converters.Add(new Vector4Converter());
        _serializeOptions.Converters.Add(new QuaternionConverter());
    }

    public static T Deserialize<T>(string json)
    {
        T? obj = System.Text.Json.JsonSerializer.Deserialize<T>(json, _serializeOptions);
        if (obj == null)
            throw new Exception($"Failed to deserialize");

        return (T)obj;
    }

    public static object Deserialize(string json, Type type)
    {
        object? obj = System.Text.Json.JsonSerializer.Deserialize(json, type, _serializeOptions);
        if(obj == null)
            throw new Exception($"Failed to deserialize");

        return obj;
    }

    public static string Serialize(object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, _serializeOptions);
    }
}
