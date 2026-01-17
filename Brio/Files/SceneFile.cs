using Brio.Game.Types;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;
using MessagePack;
using System;
using System.Collections.Generic;

namespace Brio.Files;

public class SceneFileInfo : JsonDocumentBaseFileInfo<SceneFile>
{
    public override string Name => "Scene File";

    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");
    public override string Extension => ".brioscn";

}

[Serializable]
[MessagePackObject]
public class SceneFile
{
    [Key(0)] public int? Version { get; set; } = 2;

    [Key(1)] public SceneFileMetaData FileMetaData { get; set; } = new SceneFileMetaData();

    [Key(5)] public string FileType => "Brio Scene";

    [Key(6)] public List<ActorFile> Actors { get; set; } = [];

    [Key(7)] public List<GameCameraFile> GameCameras { get; set; } = [];

    [Key(8)] public SceneMetaData? MetaData { get; set; }

    [Key(9)] public EnvironmentData? EnvironmentData { get; set; }

    [Key(10)] public List<LightFile>? GameLights { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SceneFileMetaData
{
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Base64Image { get; set; }
    public TagCollection? Tags { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SceneMetaData
{
    public uint Map { get; set; }
    public ushort Territory { get; set; }
    public string? World { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class EnvironmentData
{
    public bool IsTimeFrozen;
    public long EorzeaTime;

    public int MinuteOfDay;
    public int DayOfMonth;

    public WeatherId CurrentWeather;
    public bool IsWaterFrozen;
}
