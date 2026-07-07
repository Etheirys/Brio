using System.Collections.Generic;

namespace Brio.Config;

public class QuickAccessConfiguration
{
    public int MaxRecents { get; set; } = 30;

    public Dictionary<string, List<QuickAccessEntry>> FavoritesByStore { get; set; } = [];
    public Dictionary<string, List<QuickAccessEntry>> RecentsByStore { get; set; } = [];
}

public record QuickAccessEntry(string Store, string Id, string DisplayName, uint IconId, string Data);
