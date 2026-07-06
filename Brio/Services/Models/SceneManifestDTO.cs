using MessagePack;
using System;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SceneManifestDTO
{
    public int Version { get; set; } = 1;

    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Base64Image { get; set; }

    public SceneMetaDataDTO? MetaData { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SceneMetaDataDTO
{
    public uint Map { get; set; }
    public ushort Territory { get; set; }
    public string? World { get; set; }
}
