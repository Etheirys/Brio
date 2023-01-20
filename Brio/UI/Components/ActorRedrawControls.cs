using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Brio.UI.Components;

public static class ActorRedrawControls
{
    private static bool _preservePosition = true;

    public unsafe static void Draw(GameObject gameObject)
    {
        bool redrawAllowed = ActorRedrawService.Instance.CanRedraw(gameObject);

        if(!redrawAllowed) ImGui.BeginDisabled();

        ImGui.Checkbox("Preserve Position / Rotation", ref _preservePosition);

        RedrawType redrawType = _preservePosition ? RedrawType.PreservePosition : RedrawType.None;

        if(ImGui.Button("Redraw"))
            ActorRedrawService.Instance.Redraw(gameObject, RedrawType.RedrawWeaponsOnOptimized | RedrawType.AllowOptimized | RedrawType.AllowFull | redrawType);

        if(ImGui.Button("Redraw Full"))
            ActorRedrawService.Instance.Redraw(gameObject, redrawType | RedrawType.AllowFull);

        if(!redrawAllowed) ImGui.EndDisabled();
    }
}
