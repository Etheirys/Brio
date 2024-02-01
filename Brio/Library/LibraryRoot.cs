using Brio.Config;
using Brio.Library.Actions;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Windows;
using Dalamud.Interface.Internal;
using ImGuiNET;

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

        
        float height = ImBrio.GetRemainingHeight();
        if(ImGui.BeginChild("library_info_sources", new(-1, height)))
        {
            LibrarySourcesEditor.Draw("Sources", _configurationService, _configurationService.Configuration.Library, ref _selectedSourceConfig, ref _isEditingSource);
            ImGui.EndChild();
        }
    }

    protected override string GetInternalId()
    {
        return "Root";
    }
}
