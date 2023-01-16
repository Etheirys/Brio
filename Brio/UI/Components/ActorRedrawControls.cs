using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Brio.UI.Components;

public static class ActorRedrawControls
{
    public unsafe static void Draw(GameObject gameObject)
    {
        if(ImGui.Button("Redraw"))
            Brio.ActorRedrawService.StandardRedraw(gameObject);

        ImGui.SameLine();

        if (ImGui.Button("Modern NPC Redraw"))
            Brio.ActorRedrawService.ModernNPCHackRedraw(gameObject);

        if (ImGui.Button("Legacy NPC Redraw"))
            Brio.ActorRedrawService.LegacyNPCHackRedraw(gameObject);
    }
}
