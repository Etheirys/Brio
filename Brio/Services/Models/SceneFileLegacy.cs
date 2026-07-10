using Brio.Game.Types;
using MessagePack;
using System;
using System.Collections.Generic;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject]
public class SceneFileLegacy
{
    [Key(0)] public int? Version { get; set; } = 2;

    [Key(1)] public LegacySceneFileMetaData FileMetaData { get; set; } = new LegacySceneFileMetaData();

    [Key(5)] public string FileType => "Brio Scene";

    [Key(6)] public List<ActorDTO> Actors { get; set; } = [];

    [Key(7)] public List<CameraDTO> GameCameras { get; set; } = [];

    [Key(8)] public LegacySceneMetaData? MetaData { get; set; }

    [Key(9)] public LegacyEnvironmentData? EnvironmentData { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class LegacySceneFileMetaData
{
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Base64Image { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class LegacySceneMetaData
{
    public uint Map { get; set; }
    public ushort Territory { get; set; }
    public string? World { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class LegacyEnvironmentData
{
    public bool IsTimeFrozen;
    public long EorzeaTime;

    public int MinuteOfDay;
    public int DayOfMonth;

    public WeatherId CurrentWeather;
    public bool IsWaterFrozen;
}
