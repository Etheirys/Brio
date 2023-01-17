using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Brio.UI.Components;

public static class ActorRedrawControls
{
    private static bool _preservePosition = true;

    public unsafe static void Draw(GameObject gameObject)
    {
        bool redrawAllowed = Brio.ActorRedrawService.CanRedraw;

        if (!redrawAllowed) ImGui.BeginDisabled();

        ImGui.Checkbox("Preserve Position / Rotation", ref _preservePosition);

        if(ImGui.Button("Redraw"))
            Brio.ActorRedrawService.Redraw(gameObject, RedrawType.Standard, _preservePosition);

        ImGui.SameLine();

        if (ImGui.Button("Modern NPC Redraw"))
            Brio.ActorRedrawService.Redraw(gameObject, RedrawType.ForceNPCAppearance, _preservePosition);

        if (!redrawAllowed) ImGui.EndDisabled();
    }
}
