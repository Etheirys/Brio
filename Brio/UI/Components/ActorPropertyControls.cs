using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Brio.UI.Components;

public static class ActorPropertyControls
{
    public unsafe static void Draw(GameObject gameObject)
    {
        string name = gameObject.Name.ToString();
        string originalName = name;

        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Name").X);

        if (ImGui.InputText("Name", ref name, 63))
        {
            if (name != originalName)
            {
                gameObject.SetName(name);
            }
        }

        ImGui.PopItemWidth();
    }
}
