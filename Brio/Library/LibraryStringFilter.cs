using Brio.Library.Tags;
using Brio.UI.Windows;

namespace Brio.Library;

public class LibraryStringFilter : LibraryFilterBase
{
    public string? SearchString;

    public LibraryStringFilter()
        : base("Search")
    {
    }

    public override bool Filter(ILibraryEntry entry)
    {
        if(this.SearchString == null)
            return false;

        string[] querry = SearchUtility.ToQuery(this.SearchString);

        // search the tags for now, eventually move to a tag filter instead
        if (entry is ITagged tagged)
        {
            foreach (Tag tag in tagged.Tags)
            {
                if(SearchUtility.Matches(tag.Name, querry))
                    return true;

                foreach(string alias in tag.Aliases)
                {
                    if(SearchUtility.Matches(alias, querry))
                    {
                        return true;
                    }
                }
            }
        }

        return SearchUtility.Matches(entry.Name, querry);
    }
}
