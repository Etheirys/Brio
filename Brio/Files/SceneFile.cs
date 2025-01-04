using System;
using System.Collections.Generic;
using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;


namespace Brio.Files;

internal class SceneFileInfo : JsonDocumentBaseFileInfo<SceneFile>
{
    public override string Name => "Scene File";

    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");
    public override string Extension => ".brioscn";

}
internal class ProjectFileInfo : JsonDocumentBaseFileInfo<SceneFile>
{
    public override string Name => "Brio Project";

    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");
    public override string Extension => ".briosln";

}

[Serializable]
internal class SceneFile : JsonDocumentBase
{
    public string FileType { get; set; } = "Brio Scene";

    public List<ActorFile> Actors { get; set; } = [];

    public GameCameraFile? GameCamera { get; set; }

    public XATCameraFile? XATCamera { get; set; }

    public SceneMetaData? MetaData { get; set; }
}

[Serializable]
internal class SceneMetaData
{
    public uint Map { get; set; }
    public ushort Territory { get; set; }
    public string? World { get; set; }
}
