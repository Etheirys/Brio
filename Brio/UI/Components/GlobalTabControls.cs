using ImGuiNET;

namespace Brio.UI.Components;

public static class GlobalTabControls
{
    public static void Draw()
    {
        if(ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool alwaysNPCHack = Brio.Configuration.ApplyNPCHack == Config.ApplyNPCHack.Always;
            bool npcHackEnabled = alwaysNPCHack || Brio.RenderHooks.ApplyNPCOverride;
            bool wasNpcHackEnabled = npcHackEnabled;
            if (alwaysNPCHack) ImGui.BeginDisabled();
            if(ImGui.Checkbox("Allow NPC Appearance", ref npcHackEnabled))
            {
                if(npcHackEnabled != wasNpcHackEnabled)
                    Brio.RenderHooks.ApplyNPCOverride = npcHackEnabled;
            }
            if (alwaysNPCHack) ImGui.EndDisabled();
        }
    }
}
