using Dalamud.Interface.Internal;

namespace Brio.Library;

public abstract class LibraryProviderBase : LibraryEntryBase
{
    private string _name;
    private IDalamudTextureWrap _icon;

    public LibraryProviderBase(string name, IDalamudTextureWrap icon)
    {
        _name = name;
        _icon = icon;
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;

    public abstract void Scan();
}
