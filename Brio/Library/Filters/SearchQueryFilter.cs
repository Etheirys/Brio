namespace Brio.Library.Filters;

public class SearchQueryFilter : FilterBase
{
    public string[]? Query;

    public SearchQueryFilter()
        : base("Search")
    {
    }

    public override void Clear()
    {
        this.Query = null;
    }

    public override bool Filter(EntryBase entry)
    {
        if(this.Query == null)
            return false;

        return entry.Search(this.Query);
    }
}
