using Brio.Config;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow() : base($"{Brio.PluginName} Settings", ImGuiWindowFlags.NoResize)
        {
            Size = new Vector2(300, 400);
        }

        public override void Draw()
        {
            if(ImGui.BeginTabBar("brio_settings"))
            {
                DrawInterfaceSettings();
                DrawHookSettings();

                ImGui.EndTabBar();
            }
        }

        private void DrawInterfaceSettings()
        {
            if(ImGui.BeginTabItem("Interface"))
            {
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Open Brio").X);
                var selectedBrioOpenBehavior = Brio.Configuration.OpenBrioBehavior;
                if (ImGui.BeginCombo("Open Brio##OpenBrioBehavior", $"{selectedBrioOpenBehavior}"))
                {
                    foreach (var openBrioBehavior in Enum.GetValues<OpenBrioBehavior>())
                    {
                        if (ImGui.Selectable($"{openBrioBehavior}", openBrioBehavior == selectedBrioOpenBehavior))
                            Brio.Configuration.OpenBrioBehavior = openBrioBehavior;
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();

                ImGui.Separator();

                bool previousShowInCutscenes = Brio.Configuration.ShowInCutscene;
                bool showInCutscenes = previousShowInCutscenes;
                if (ImGui.Checkbox("Show in Cutscenes", ref showInCutscenes))
                {
                    if (showInCutscenes != previousShowInCutscenes)
                    {
                        Brio.Configuration.ShowInCutscene = showInCutscenes;
                        Brio.UI.ApplyUISettings();
                    }
                }
                ImGui.Separator();

                bool previousShowWhenUIHidden = Brio.Configuration.ShowWhenUIHidden;
                bool showWhenUIHidden = previousShowWhenUIHidden;
                if (ImGui.Checkbox("Show When UI Hidden", ref showWhenUIHidden))
                {
                    if (showWhenUIHidden != previousShowWhenUIHidden)
                    {
                        Brio.Configuration.ShowWhenUIHidden = showWhenUIHidden;
                        Brio.UI.ApplyUISettings();
                    }
                }

                ImGui.EndTabItem();
            }
        }

        private void DrawHookSettings()
        {
            if (ImGui.BeginTabItem("Hooks"))
            {
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Allow NPC Appearance").X);
                var selectedNPCHack = Brio.Configuration.ApplyNPCHack;
                if (ImGui.BeginCombo("Allow NPC Appearance##ApplyNPCHack", $"{selectedNPCHack}"))
                {
                    foreach (var applyNpcHackType in Enum.GetValues<ApplyNPCHack>())
                    {
                        if (ImGui.Selectable($"{applyNpcHackType}", applyNpcHackType == selectedNPCHack))
                            Brio.Configuration.ApplyNPCHack = applyNpcHackType;
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();

                ImGui.EndTabItem();
            }
        }
    }
}
 