using ImGuiNET;

namespace Brio.UI.Components;
public static class WorldTabControls
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
