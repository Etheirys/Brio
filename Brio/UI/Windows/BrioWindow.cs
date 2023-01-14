using Brio.UI.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows;

public class BrioWindow : Window
{
    public BrioWindow() : base(Brio.PluginName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Size = new Vector2(250, -1);
    }

    public override void Draw()
    {
        GPoseGlobalControls.Draw();
    }
}
