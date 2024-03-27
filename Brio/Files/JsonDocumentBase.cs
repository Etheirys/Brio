using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using System;

namespace Brio.Files;

internal abstract class JsonDocumentBaseFileInfo<T> : FileTypeInfoBase<T>
{
    public override object? Load(string filePath) => ResourceProvider.Instance.GetFileDocument<T>(filePath);
}

[Serializable]
internal abstract class JsonDocumentBase : IFileMetadata
{
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Base64Image { get; set; }
    public TagCollection? Tags { get; set; }

    public virtual void GetAutoTags(ref TagCollection tags)
    {
        if(this.Author != null)
        {
            tags.Add(this.Author);
        }
    }
}
