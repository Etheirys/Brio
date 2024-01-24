using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataMountSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataMountSource(GameDataProvider lumina)
        : base("Mounts", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, mount) in _lumina.Mounts)
        {
            string rowName = $"Mount {mount.RowId}";

            var entry = new GameDataAppearanceEntry(this, mount.Singular ?? rowName, mount.Icon, mount);
            entry.Tags.Add("Mount");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
