using Brio.MCDF.API.Data.Enum;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.MCDF.API.Data;

[MessagePackObject(keyAsPropertyName: true)]
public class CharacterData
{
    public CharacterData()
    {
        DataHash = new(() =>
        {
            var json = JsonSerializer.Serialize(this);
            return Core.BLAKE3.HashBytes(Encoding.UTF8.GetBytes(json).AsSpan());
        });
    }

    public Dictionary<ObjectKind, string> CustomizePlusData { get; set; } = [];

    [JsonIgnore]
    public Lazy<string> DataHash { get; }

    public Dictionary<ObjectKind, List<FileReplacementData>> FileReplacements { get; set; } = [];
    public Dictionary<ObjectKind, string> GlamourerData { get; set; } = [];

    public string HeelsData { get; set; } = string.Empty;
    public string HonorificData { get; set; } = string.Empty;
    public string ManipulationData { get; set; } = string.Empty;
    public string MoodlesData { get; set; } = string.Empty;
    public string PetNamesData { get; set; } = string.Empty;
}
