using Brio.Config;
using Brio.IPC;
using Brio.Web;
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
                if(ImGui.CollapsingHeader("Brio Window", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Open Brio").X);
                    var selectedBrioOpenBehavior = ConfigService.Configuration.OpenBrioBehavior;
                    if(ImGui.BeginCombo("Open Brio##OpenBrioBehavior", $"{selectedBrioOpenBehavior}"))
                    {
                        foreach(var openBrioBehavior in Enum.GetValues<OpenBrioBehavior>())
                        {
                            if(ImGui.Selectable($"{openBrioBehavior}", openBrioBehavior == selectedBrioOpenBehavior))
                                ConfigService.Configuration.OpenBrioBehavior = openBrioBehavior;
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.PopItemWidth();
                }

                ImGui.Separator();

                if(ImGui.CollapsingHeader("Game State", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool previousShowInCutscenes = ConfigService.Configuration.ShowInCutscene;
                    bool showInCutscenes = previousShowInCutscenes;
                    if(ImGui.Checkbox("Show in Cutscenes", ref showInCutscenes))
                    {
                        if(showInCutscenes != previousShowInCutscenes)
                        {
                            ConfigService.Configuration.ShowInCutscene = showInCutscenes;
                            UIService.Instance.ApplyUISettings();
                        }
                    }

                    bool previousShowWhenUIHidden = ConfigService.Configuration.ShowWhenUIHidden;
                    bool showWhenUIHidden = previousShowWhenUIHidden;
                    if(ImGui.Checkbox("Show When UI Hidden", ref showWhenUIHidden))
                    {
                        if(showWhenUIHidden != previousShowWhenUIHidden)
                        {
                            ConfigService.Configuration.ShowWhenUIHidden = showWhenUIHidden;
                            UIService.Instance.ApplyUISettings();
                        }
                    }
                }

                ImGui.EndTabItem();
            }
        }

        private void DrawHookSettings()
        {
            if(ImGui.BeginTabItem("Hooks"))
            {
                if(ImGui.CollapsingHeader("Render Hooks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Allow NPC Appearance").X);
                    var selectedNPCHack = ConfigService.Configuration.ApplyNPCHack;
                    if(ImGui.BeginCombo("Allow NPC Appearance##ApplyNPCHack", $"{selectedNPCHack}"))
                    {
                        foreach(var applyNpcHackType in Enum.GetValues<ApplyNPCHack>())
                        {
                            if(ImGui.Selectable($"{applyNpcHackType}", applyNpcHackType == selectedNPCHack))
                                ConfigService.Configuration.ApplyNPCHack = applyNpcHackType;
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
            if(ImGui.BeginTabItem("Integrations"))
            {
                if(ImGui.CollapsingHeader("Brio IPC", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool previousBrioIpcEnabled = ConfigService.Configuration.AllowBrioIPC;
                    bool enableBrioIpc = previousBrioIpcEnabled;
                    if(ImGui.Checkbox("Enable Brio IPC", ref enableBrioIpc))
                    {
                        if(enableBrioIpc != previousBrioIpcEnabled)
                        {
                            ConfigService.Configuration.AllowBrioIPC = enableBrioIpc;
                        }
                    }
                }
                ImGui.Text($"Brio IPC Status: {(BrioIPCService.Instance.IsIPCEnabled ? "Active" : "Inactive")}");

                ImGui.Separator();

                if(ImGui.CollapsingHeader("Web API", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool previousEnableWebApi = ConfigService.Configuration.AllowWebAPI;
                    bool enableWebApi = previousEnableWebApi;
                    if(ImGui.Checkbox("Enable Web API", ref enableWebApi))
                    {
                        if(enableWebApi != previousEnableWebApi)
                        {
                            ConfigService.Configuration.AllowWebAPI = enableWebApi;
                        }
                    }
                }
                ImGui.Text($"Web API Status: {(WebService.Instance.IsRunning ? "Active" : "Inactive")}");

                ImGui.Separator();

                if(ImGui.CollapsingHeader("Penumbra", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool previousEnablePenumbra = ConfigService.Configuration.AllowPenumbraIntegration;
                    bool enablePenumbra = previousEnablePenumbra;
                    if(ImGui.Checkbox("Allow Penumbra Integration", ref enablePenumbra))
                    {
                        if(enablePenumbra != previousEnablePenumbra)
                        {
                            ConfigService.Configuration.AllowPenumbraIntegration = enablePenumbra;
                            PenumbraIPCService.Instance.RefreshPenumbraStatus();
                        }
                    }

                    if(!enablePenumbra) ImGui.BeginDisabled();
                    ImGui.Text($"Penumbra Status: {(PenumbraIPCService.Instance.IsPenumbraEnabled ? "Active" : "Inactive")}");
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString()))
                    {
                        PenumbraIPCService.Instance.RefreshPenumbraStatus();
                        if(!PenumbraIPCService.Instance.IsPenumbraEnabled)
                            Dalamud.ToastGui.ShowError("Brio/Penumbra integration failed.\nEnsure Penumbra is enabled and up to date.");
                    }
                    ImGui.PopFont();
                    if(!enablePenumbra) ImGui.EndDisabled();
                }
                ImGui.EndTabItem();
            }
        }
    }
}
