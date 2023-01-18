using Brio.Config;
using Dalamud.Interface;
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
                DrawIntegrationSettings();
                DrawHookSettings();

                ImGui.EndTabBar();
            }
        }

        private void DrawInterfaceSettings()
        {
            if(ImGui.BeginTabItem("Interface"))
            {
                if (ImGui.CollapsingHeader("Brio Window", ImGuiTreeNodeFlags.DefaultOpen))
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
                }

                ImGui.Separator();

                if (ImGui.CollapsingHeader("Game State", ImGuiTreeNodeFlags.DefaultOpen))
                {
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
                }

                ImGui.EndTabItem();
            }
        }

        private void DrawHookSettings()
        {
            if (ImGui.BeginTabItem("Hooks"))
            {
                if (ImGui.CollapsingHeader("Render Hooks", ImGuiTreeNodeFlags.DefaultOpen))
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
                }

                ImGui.EndTabItem();
            }
        }

        private void DrawIntegrationSettings()
        {
            if (ImGui.BeginTabItem("Integrations"))
            {

                if (ImGui.CollapsingHeader("Penumbra", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool previousEnablePenumbra = Brio.Configuration.AllowPenumbraIntegration;
                    bool enablePenumbra = previousEnablePenumbra;
                    if (ImGui.Checkbox("Allow Penumbra Integration", ref enablePenumbra))
                    {
                        if (enablePenumbra != previousEnablePenumbra)
                        {
                            Brio.Configuration.AllowPenumbraIntegration = enablePenumbra;
                            Brio.PenumbraIPC.RefreshPenumbraStatus();
                        }
                    }

                    if (!enablePenumbra) ImGui.BeginDisabled();
                    ImGui.Text($"Penumbra Status: {(Brio.PenumbraIPC.IsPenumbraEnabled ? "Active" : "Inactive")}");
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Redo.ToIconString()))
                    {
                        Brio.PenumbraIPC.RefreshPenumbraStatus();
                        if (!Brio.PenumbraIPC.IsPenumbraEnabled)
                            Dalamud.ToastGui.ShowError("Brio/Penumbra integration failed.\nEnsure Penumbra is enabled and up to date.");
                    }
                    ImGui.PopFont();
                    if (!enablePenumbra) ImGui.EndDisabled();
                }
                ImGui.EndTabItem();
            }
        }
    }
}
 