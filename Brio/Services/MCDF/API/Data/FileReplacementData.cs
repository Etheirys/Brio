using MessagePack;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brio.MCDF.API.Data;

[MessagePackObject(keyAsPropertyName: true)]
public class FileReplacementData
{
    public FileReplacementData()
    {
        DataHash = new(() =>
        {
            var json = JsonSerializer.Serialize(this);
            return Core.BLAKE3.HashBytes(Encoding.UTF8.GetBytes(json).AsSpan());
        });
    }

    [JsonIgnore]
    public Lazy<string> DataHash { get; }
    public string FileSwapPath { get; set; } = string.Empty;
    public string[] GamePaths { get; set; } = [];
    public string Hash { get; set; } = string.Empty;
}
