using Brio.Library.Filters;

namespace Brio.Library;

public class LibraryFavoritesFilter : FilterBase
{
    public LibraryFavoritesFilter()
        : base("Favorites")
    {
    }

    public override void Clear()
    {
    }

    public override bool Filter(ILibraryEntry entry)
    {
        // no favorites system yet!
        return false;
    }
}
