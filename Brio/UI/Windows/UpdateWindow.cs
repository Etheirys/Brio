using Brio.Config;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows;

internal class UpdateWindow : Window
{

    private readonly ConfigurationService _configurationService;
    private string _changelogTest;

    const float _closeButtonWidth = 210f;

    public UpdateWindow(ConfigurationService configurationService) : base($" {Brio.Name} Changelog###brio_update_window",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_update_namespace";

        _configurationService = configurationService;

        Size = new Vector2(630, 535);

        ShowCloseButton = false;

        _changelogTest = ResourceProvider.Instance.GetRawResourceString("Data.Changelog.txt");
    }

    bool _scrollToTop = false;
    public override void OnOpen()
    {
        _scrollToTop = true;
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 1f;

        if(ImGui.BeginChild("###brio_update_text", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetRemainingHeight() - 35f), true, Flags = ImGuiWindowFlags.NoSavedSettings))
        {
            if(_scrollToTop)
            {
                _scrollToTop = false;
                ImGui.SetScrollHereY(0);
            }
            ImGui.PushTextWrapPos(segmentSize);
            ImGui.TextWrapped(_changelogTest);
            ImGui.PopTextWrapPos();

            ImGui.EndChild();
        }

        ImGui.SetCursorPosX(((ImBrio.GetRemainingWidth() - _closeButtonWidth) / 2));

        if(ImBrio.Button("Close", Dalamud.Interface.FontAwesomeIcon.SquareXmark, new Vector2(_closeButtonWidth, 0)))
        {
            this.IsOpen = false;
        }
    }

}
