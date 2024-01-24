using Brio.Library.Tags;

namespace Brio.Library.Filters;

public class TagFilter : FilterBase
{
    public TagCollection? Tags;

    public TagFilter()
        : base("Tags")
    {
    }

    public override void Clear()
    {
        this.Tags = null;
    }

    public void Add(Tag tag)
    {
        if(this.Tags == null)
            this.Tags = new();

        this.Tags.Add(tag);
    }

    public override bool Filter(ILibraryEntry entry)
    {
        if(this.Tags == null)
            return true;

        if(entry.Tags == null)
            return false;

        if(entry.Tags.Contains(Tags))
            return true;

        return false;
    }
}
