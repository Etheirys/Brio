using Brio.Library.Filters;

namespace Brio.Library;

internal class LibraryFavoritesFilter : FilterBase
{
    public LibraryFavoritesFilter()
        : base("Favorites")
    {
    }

    public override void Clear()
    {
    }

    public override bool Filter(EntryBase entry)
    {
        // no favorites system yet!
        return false;
    }
}
