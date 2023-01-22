using ImGuiNET;

namespace Brio.UI.Components.World;
public static class WorldTab
{
    public static void Draw()
    {

        if(ImGui.CollapsingHeader("Time", ImGuiTreeNodeFlags.DefaultOpen))
        {
            TimeControls.Draw();
        }

        if(ImGui.CollapsingHeader("Weather", ImGuiTreeNodeFlags.DefaultOpen))
        {
            WeatherControls.Draw();
        }
    }
}
