using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Brio.Files.AnamnesisCharaFile;

namespace Brio.Files.Converters;

internal class LegacyGlassesSaveConverter : JsonConverter<GlassesSave>
{
    public override GlassesSave Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            ushort? str = reader.GetUInt16();
                  
            return new GlassesSave { GlassesId = (byte)str.Value };
        }
        catch(Exception)
        {
            Brio.Log.Fatal($"Loaded GS Exception -- {typeToConvert}");

            return new GlassesSave { GlassesId = 0 };
        }
    }

    public override void Write(Utf8JsonWriter writer, GlassesSave value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
