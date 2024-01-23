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

        // way too basic.
        return entry.Name.ToLower().Contains(this.SearchString.ToLower());
    }
}
