using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using MessagePack;
using System;

namespace Brio.Files;

public abstract class JsonDocumentBaseFileInfo<T> : FileTypeInfoBase<T>
{
    public override object? Load(string filePath) => ResourceProvider.Instance.GetFileDocument<T>(filePath);
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
[Union(0, typeof(SceneFile))]
[Union(1, typeof(AnamnesisCharaFile))]
[Union(2, typeof(PoseFile))]
public abstract class JsonDocumentBase : IFileMetadata
{
    [Key(0)] public string? Author { get; set; }
    [Key(1)] public string? Description { get; set; }
    [Key(2)] public string? Version { get; set; }
    [Key(3)] public string? Base64Image { get; set; }
    [Key(4)] public TagCollection? Tags { get; set; }

    public virtual void GetAutoTags(ref TagCollection tags)
    {
        if(this.Author != null)
        {
            tags.Add(this.Author);
        }
    }
}
