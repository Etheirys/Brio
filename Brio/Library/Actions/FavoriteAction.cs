using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class FavoriteAction : EntryActionBase<ItemEntryBase>
{
    public FavoriteAction()
        : base(false)
    {
    }

    public bool IsFavorite { get; set; } = false;

    public override string GetLabel(EntryBase entry)
    {
        return IsFavorite ? "Unfavorite" : "Favorite";
    }

    protected override Task InvokeAsync(ItemEntryBase entry)
    {
        IsFavorite = !IsFavorite;

        return Task.CompletedTask;
    }
}
