using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.Files.Converters;

public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString() ?? throw new Exception("Cannot convert null to Vector3");
        string[] parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

        if(parts.Length != 4)
            throw new FormatException();

        Quaternion q = default;
        q.X = float.Parse(parts[0], CultureInfo.InvariantCulture);
        q.Y = float.Parse(parts[1], CultureInfo.InvariantCulture);
        q.Z = float.Parse(parts[2], CultureInfo.InvariantCulture);
        q.W = float.Parse(parts[3], CultureInfo.InvariantCulture);
        return q;
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        var newString = value.X.ToString(CultureInfo.InvariantCulture) + ", " + value.Y.ToString(CultureInfo.InvariantCulture) + ", " + value.Z.ToString(CultureInfo.InvariantCulture) + ", " + value.W.ToString(CultureInfo.InvariantCulture);
        writer.WriteStringValue(newString);
    }
}
