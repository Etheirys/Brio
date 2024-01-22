using Dalamud.Interface;

namespace Brio.Library;

internal abstract class CategoryBase
{
    public abstract string Title { get; }
}

internal class PosesCategory : CategoryBase
{
    public override string Title => "Poses";
}

internal class CharactersCategory : CategoryBase
{
    public override string Title => "Characters";
}

internal class FavoritesCategory : CategoryBase
{
    public override string Title => "Favorites";
}
