namespace Brio.Library;

public class LibraryFavoritesFilter : LibraryFilterBase
{
    public LibraryFavoritesFilter()
        : base("Favorites")
    {
    }

    public override bool Filter(ILibraryEntry entry)
    {
        // no favorites system yet!
        return false;
    }
}
