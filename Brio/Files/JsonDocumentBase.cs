using Brio.Library.Sources;
using Brio.Library.Tags;

namespace Brio.Files;
internal abstract class JsonDocumentBase : IFile
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
