using Brio.Config;
using Brio.Game.Render;
using ImGuiNET;

namespace Brio.UI.Components;

public static class GlobalTabControls
{
    public static void Draw()
    {
        if(ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool alwaysNPCHack = ConfigService.Configuration.ApplyNPCHack == Config.ApplyNPCHack.Always;
            bool npcHackEnabled = alwaysNPCHack || RenderHookService.Instance.ApplyNPCOverride;
            bool wasNpcHackEnabled = npcHackEnabled;
            if (alwaysNPCHack) ImGui.BeginDisabled();
            if(ImGui.Checkbox("Allow NPC Appearance", ref npcHackEnabled))
            {
                if(npcHackEnabled != wasNpcHackEnabled)
                    RenderHookService.Instance.ApplyNPCOverride = npcHackEnabled;
            }
            if (alwaysNPCHack) ImGui.EndDisabled();
        }
    }
}
