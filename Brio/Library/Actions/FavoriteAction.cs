using Brio.Config;
using Brio.UI.Windows;
using Dalamud.Interface;
using System.Threading.Tasks;

namespace Brio.Library.Actions;

internal class FavoriteAction : EntryActionBase<ItemEntryBase>
{
    public FavoriteAction()
        : base(false)
    {
    }

    public bool GetIsFavorite(EntryBase entry)
    {
        ConfigurationService configurationService = GetService<ConfigurationService>();
        return configurationService.Configuration.Library.Favorites.Contains(entry.Identifier);
    }

    public void SetIsFavorite(EntryBase entry, bool favorite)
    {
        ConfigurationService configurationService = GetService<ConfigurationService>();
        var favorites = configurationService.Configuration.Library.Favorites;

        if (favorite)
        {
            favorites.Add(entry.Identifier);
        }
        else
        {
            favorites.Remove(entry.Identifier);
        }
        
        configurationService.Save();
    }

    public override string GetLabel(EntryBase entry)
    {
        return GetIsFavorite(entry) ? "Unfavorite" : "Favorite";
    }

    public override FontAwesomeIcon GetIcon(EntryBase entry)
    {
        return GetIsFavorite(entry) ? FontAwesomeIcon.HeartBroken : FontAwesomeIcon.Heart;
    }

    protected override Task InvokeAsync(ItemEntryBase entry)
    {
        SetIsFavorite(entry, !GetIsFavorite(entry));
        GetService<LibraryWindow>().Refresh(true);

        return Task.CompletedTask;
    }
}
