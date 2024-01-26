using Brio.Library.Tags;
using System.Text;

namespace Brio.Library.Filters;

internal class SearchQueryFilter : FilterBase
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
        if(Query == null)
            return false;

        // search the tags for now, eventually move to a tag filter instead
        if(entry is ITagged tagged)
        {
            foreach(Tag tag in tagged.Tags)
            {
                if(SearchUtility.Matches(tag.Name, Query))
                    return true;

                foreach(string alias in tag.Aliases)
                {
                    if(SearchUtility.Matches(alias, Query))
                    {
                        return true;
                    }
                }
            }
        }

        return SearchUtility.Matches(entry.Name, Query);
    }

    public string GetSearchString()
    {
        if(Query == null)
            return string.Empty;

        StringBuilder sb = new();
        foreach(string str in Query)
        {
            sb.Append(str);
            sb.Append(' ');
        }

        return sb.ToString().TrimEnd(' ');
    }
}
