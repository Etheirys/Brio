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

[Serializable]
internal class SceneFile
{
    public List<ActorFile> Actors { get; set; } = [];
    
    public void AddActor(ActorFile actorFile)
    {
        Actors.Add(actorFile);
    }
}
