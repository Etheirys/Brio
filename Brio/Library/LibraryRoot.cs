using Brio.Config;
using Brio.UI.Windows;
using Dalamud.Interface.Internal;

namespace Brio.Library;

internal class LibraryRoot : GroupEntryBase
{
    private readonly LibraryManager _manager;
    private readonly ConfigurationService _configurationService;
    private LibraryConfiguration.SourceConfigBase? _selectedSourceConfig;
    private bool _isEditingSource;

    public LibraryRoot(LibraryManager manager, ConfigurationService configurationService)
        : base(null)
    {
        _manager = manager;
        _configurationService = configurationService;
    }

    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;

    public override void DrawInfo(LibraryWindow window)
    {
        base.DrawInfo(window);
    }

    protected override string GetInternalId()
    {
        return "Root";
    }
}
