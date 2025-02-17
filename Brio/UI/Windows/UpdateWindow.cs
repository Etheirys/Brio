using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Brio.UI.Windows;

public class UpdateWindow : Window
{
    private readonly List<string> _changelogTest = [];
    private const float _closeButtonWidth = 210f;

    public UpdateWindow() : base($" {Brio.Name} Changelog###brio_update_window")
    {
        Namespace = "brio_update_namespace";

        Size = new Vector2(630, 635);
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse;

        ShowCloseButton = false;

        var logStream = ResourceProvider.Instance.GetRawResourceStream("Data.Changelog.txt");

        using(var streamReader = new StreamReader(logStream, Encoding.UTF8, true, 128))
        {
            string? line;
            while((line = streamReader.ReadLine()) is not null)
            {
                _changelogTest.Add(line);
            }
        }
    }

    bool _scrollToTop = false;
    public override void OnOpen()
    {
        _scrollToTop = true;
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 1f;

        if(ImGui.BeginChild("###brio_update_text", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetRemainingHeight() - 35f), true, Flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
        {
            if(_scrollToTop)
            {
                _scrollToTop = false;
                ImGui.SetScrollHereY(0);
            }
            ImGui.PushTextWrapPos(segmentSize);
            for(int i = 0; i < _changelogTest.Count; i++)
            {
                ImGui.TextWrapped(_changelogTest[i]);
            }
            ImGui.PopTextWrapPos();

            ImGui.EndChild();
        }

        ImGui.SetCursorPosX(((ImBrio.GetRemainingWidth() - _closeButtonWidth) / 2));

        if(ImBrio.Button("Close", Dalamud.Interface.FontAwesomeIcon.SquareXmark, new Vector2(_closeButtonWidth, 0), centerTest: true))
        {
            this.IsOpen = false;
        }
    }

}
