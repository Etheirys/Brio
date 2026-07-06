using Brio.Config;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Services;

public class QuickAccessService(ConfigurationService configurationService)  // TODO (Ken) I want to move this outside the Configuration and do more with it, but thats for later me
{
    private readonly ConfigurationService _configurationService = configurationService;

    private QuickAccessConfiguration Config 
        => _configurationService.Configuration.QuickAccess;

    public bool IsFavorite(string surface, string id)
        => Config.FavoritesByStore.TryGetValue(surface, out var list) && list.Any(e => e.Id == id);

    public void ToggleFavorite(QuickAccessEntry entry)
    {
        if(!Config.FavoritesByStore.TryGetValue(entry.Store, out var list))
            Config.FavoritesByStore[entry.Store] = list = [];

        var existing = list.FindIndex(e => e.Id == entry.Id);
        if(existing >= 0)
            list.RemoveAt(existing);
        else
            list.Add(entry);

        _configurationService.ApplyChange();
    }

    public IReadOnlyList<QuickAccessEntry> GetFavorites(string surface)
        => Config.FavoritesByStore.TryGetValue(surface, out var list) ? list : [];

    public void PushRecent(QuickAccessEntry entry)
    {
        if(!Config.RecentsByStore.TryGetValue(entry.Store, out var list))
            Config.RecentsByStore[entry.Store] = list = [];

        list.RemoveAll(e => e.Id == entry.Id);
        list.Insert(0, entry);

        var max = Config.MaxRecents < 1 ? 1 : Config.MaxRecents;
        if(list.Count > max)
            list.RemoveRange(max, list.Count - max);

        _configurationService.ApplyChange();
    }

    public IReadOnlyList<QuickAccessEntry> GetRecents(string surface)
        => Config.RecentsByStore.TryGetValue(surface, out var list) ? list : [];
}
