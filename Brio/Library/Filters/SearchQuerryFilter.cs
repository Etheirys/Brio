using Brio.Library.Tags;

namespace Brio.Library.Filters;

public class SearchQuerryFilter : FilterBase
{
    public string[]? Querry;

    public SearchQuerryFilter()
        : base("Search")
    {
    }

    public override void Clear()
    {
        this.Querry = null;
    }

    public override bool Filter(ILibraryEntry entry)
    {
        if(Querry == null)
            return false;

        // search the tags for now, eventually move to a tag filter instead
        if(entry is ITagged tagged)
        {
            foreach(Tag tag in tagged.Tags)
            {
                if(SearchUtility.Matches(tag.Name, Querry))
                    return true;

                foreach(string alias in tag.Aliases)
                {
                    if(SearchUtility.Matches(alias, Querry))
                    {
                        return true;
                    }
                }
            }
        }

        return SearchUtility.Matches(entry.Name, Querry);
    }
}
