using Brio.Config;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public LibraryWindow(ConfigurationService configurationService)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";
        this.Size = new(400, 450);

        _configurationService = configurationService;
    }

    public override void Draw()
    {
        using(ImRaii.PushId("brio_library"))
        {
            using(var tab = ImRaii.TabBar("###brio_library_tabs"))
            {
                if(tab.Success)
                {
                    DrawPosesTab();
                }
            }
        }
    }

    private void DrawPosesTab()
    {
        using(var tab = ImRaii.TabItem("Poses"))
        {
            if(tab.Success)
            {
            }
        }
    }
}
