using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;

namespace Brio.UI.Components.Actor;

public static class RedrawControls
{
    private static RedrawType _redrawType = RedrawType.AllowOptimized | RedrawType.AllowFull | RedrawType.PreservePosition | RedrawType.ForceAllowNPCAppearance;

    public unsafe static void Draw(GameObject gameObject)
    {
        bool redrawAllowed = ActorRedrawService.Instance.CanRedraw(gameObject);

        if(!redrawAllowed) ImGui.BeginDisabled();

        bool preservePosition = _redrawType.HasFlag(RedrawType.PreservePosition);
        ImGui.Checkbox("Preserve Position / Rotation", ref preservePosition);
        if(preservePosition)
            _redrawType |= RedrawType.PreservePosition;
        else
            _redrawType &= ~RedrawType.PreservePosition;

        bool allowFull = _redrawType.HasFlag(RedrawType.AllowFull);
        ImGui.Checkbox("Allow Full", ref allowFull);
        if(allowFull)
            _redrawType |= RedrawType.AllowFull;
        else
            _redrawType &= ~RedrawType.AllowFull;

        bool allowOptimized = _redrawType.HasFlag(RedrawType.AllowOptimized);
        ImGui.Checkbox("Allow Optimized", ref allowOptimized);
        if(allowOptimized)
            _redrawType |= RedrawType.AllowOptimized;
        else
            _redrawType &= ~RedrawType.AllowOptimized;

        bool forceAllowNPC = _redrawType.HasFlag(RedrawType.ForceAllowNPCAppearance);
        ImGui.Checkbox("Force Allow NPC Appearance", ref forceAllowNPC);
        if(forceAllowNPC)
            _redrawType |= RedrawType.ForceAllowNPCAppearance;
        else
            _redrawType &= ~RedrawType.ForceAllowNPCAppearance;

        bool forceWeapon = _redrawType.HasFlag(RedrawType.ForceRedrawWeaponsOnOptimized);
        ImGui.Checkbox("Force Weapon Redraw", ref forceWeapon);
        if(forceWeapon)
            _redrawType |= RedrawType.ForceRedrawWeaponsOnOptimized;
        else
            _redrawType &= ~RedrawType.ForceRedrawWeaponsOnOptimized;


        if(ImGui.Button("Redraw##button"))
        {
            var result = ActorRedrawService.Instance.Redraw(gameObject, _redrawType);
            if(result == RedrawResult.Failed)
                Dalamud.ToastGui.ShowError("Failed to redraw actor.");
        }


        if(!redrawAllowed) ImGui.EndDisabled();
    }
}
