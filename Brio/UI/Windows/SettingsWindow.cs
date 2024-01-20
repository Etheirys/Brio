using Brio.Config;
using Brio.Input;
using Brio.IPC;
using Brio.UI.Controls.Stateless;
using Brio.Web;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

internal class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly WebService _webService;
    private readonly BrioIPCService _brioIPCService;

    public SettingsWindow(ConfigurationService configurationService, PenumbraService penumbraService, GlamourerService glamourerService, WebService webService, BrioIPCService brioIPCService) : base($"{Brio.Name} Settings###brio_settings_window", ImGuiWindowFlags.NoResize)
    {
        Namespace = "brio_settings_namespace";

        _configurationService = configurationService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _webService = webService;
        _brioIPCService = brioIPCService;

        Size = new Vector2(400, 450);
    }

    public override void Draw()
    {
        using(ImRaii.PushId("brio_settings"))
        {
            using(var tab = ImRaii.TabBar("###brio_settings_tabs"))
            {
                if(tab.Success)
                {
                    DrawInterfaceTab();
                    DrawIPCTab();
                    DrawAppearanceTab();
                    DrawPosingTab();
                    DrawWorldTab();
                    DrawKeysTab();
                }
            }
        }
    }

    private void DrawInterfaceTab()
    {
        using(var tab = ImRaii.TabItem("Interface"))
        {
            if(tab.Success)
            {
                if(ImGui.CollapsingHeader("Window", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawOpenBrioSetting();
                    DrawHideSettings();
                }

                if(ImGui.CollapsingHeader("Display", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawDisplaySettings();
                }
            }
        }
    }

    private void DrawOpenBrioSetting()
    {
        var selectedBrioOpenBehavior = _configurationService.Configuration.Interface.OpenBrioBehavior;
        const string label = "Open Brio";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
        using(var combo = ImRaii.Combo(label, selectedBrioOpenBehavior.ToString()))
        {
            if(combo.Success)
            {
                foreach(var openBrioBehavior in Enum.GetValues<OpenBrioBehavior>())
                {
                    if(ImGui.Selectable($"{openBrioBehavior}", openBrioBehavior == selectedBrioOpenBehavior))
                    {
                        _configurationService.Configuration.Interface.OpenBrioBehavior = openBrioBehavior;
                        _configurationService.ApplyChange();
                    }
                }
            }
        }
    }

    private void DrawHideSettings()
    {
        bool showInGPose = _configurationService.Configuration.Interface.ShowInGPose;
        if(ImGui.Checkbox("Show in GPose", ref showInGPose))
        {
            _configurationService.Configuration.Interface.ShowInGPose = showInGPose;
            _configurationService.ApplyChange();
        }

        bool showInCutscene = _configurationService.Configuration.Interface.ShowInCutscene;
        if(ImGui.Checkbox("Show in Cutscenes", ref showInCutscene))
        {
            _configurationService.Configuration.Interface.ShowInCutscene = showInCutscene;
            _configurationService.ApplyChange();
        }

        bool showWhenUIHidden = _configurationService.Configuration.Interface.ShowWhenUIHidden;
        if(ImGui.Checkbox("Show when UI Hidden", ref showWhenUIHidden))
        {
            _configurationService.Configuration.Interface.ShowWhenUIHidden = showWhenUIHidden;
            _configurationService.ApplyChange();
        }
    }

    private void DrawDisplaySettings()
    {
        bool censorActorNames = _configurationService.Configuration.Interface.CensorActorNames;
        if(ImGui.Checkbox("Censor Actor Names", ref censorActorNames))
        {
            _configurationService.Configuration.Interface.CensorActorNames = censorActorNames;
            _configurationService.ApplyChange();
        }
    }

    private void DrawIPCTab()
    {
        using(var tab = ImRaii.TabItem("IPC"))
        {
            if(tab.Success)
            {
                DrawBrioIPC();
                DrawThirdPartyIPC();
            }
        }
    }

    private void DrawThirdPartyIPC()
    {
        if(ImGui.CollapsingHeader("Third-Party", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enablePenumbra = _configurationService.Configuration.IPC.AllowPenumbraIntegration;
            if(ImGui.Checkbox("Allow Penumbra Integration", ref enablePenumbra))
            {
                _configurationService.Configuration.IPC.AllowPenumbraIntegration = enablePenumbra;
                _configurationService.ApplyChange();
            }

            using(ImRaii.Disabled(!enablePenumbra))
            {
                ImGui.Text($"Penumbra Status: {(_penumbraService.IsPenumbraAvailable ? "Active" : "Inactive")}");
                ImGui.SameLine();
                ImBrio.FontIconButton("refresh_penumbra", FontAwesomeIcon.Sync, "Refresh Penumbra Status");
            }

            bool enableGlamourer = _configurationService.Configuration.IPC.AllowGlamourerIntegration;
            if(ImGui.Checkbox("Allow Glamourer Integration", ref enableGlamourer))
            {
                _configurationService.Configuration.IPC.AllowGlamourerIntegration = enableGlamourer;
                _configurationService.ApplyChange();
            }

            using(ImRaii.Disabled(!enableGlamourer))
            {
                ImGui.Text($"Glamourer Status: {(_glamourerService.IsGlamourerAvailable ? "Active" : "Inactive")}");
                ImGui.SameLine();
                ImBrio.FontIconButton("refresh_glamourer", FontAwesomeIcon.Sync, "Refresh Glamourer Status");
            }
        }
    }

    private void DrawBrioIPC()
    {

        if(ImGui.CollapsingHeader("Brio", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableBrioIpc = _configurationService.Configuration.IPC.EnableBrioIPC;
            if(ImGui.Checkbox("Enable Brio IPC", ref enableBrioIpc))
            {
                _configurationService.Configuration.IPC.EnableBrioIPC = enableBrioIpc;
                _configurationService.ApplyChange();
            }
            ImGui.Text($"Brio IPC Status: {(_brioIPCService.IsIPCEnabled ? "Active" : "Inactive")}");

            bool enableWebApi = _configurationService.Configuration.IPC.AllowWebAPI;
            if(ImGui.Checkbox("Enable Web API", ref enableWebApi))
            {
                _configurationService.Configuration.IPC.AllowWebAPI = enableWebApi;
                _configurationService.ApplyChange();
            }

            ImGui.Text($"Web API Status: {(_webService.IsRunning ? "Active" : "Inactive")}");
        }

    }

    private void DrawAppearanceTab()
    {
        using(var tab = ImRaii.TabItem("Appearance"))
        {
            if(tab.Success)
            {
                DrawNPCAppearanceHack();
            }
        }
    }

    private void DrawNPCAppearanceHack()
    {
        if(ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var allowNPCHackBehavior = _configurationService.Configuration.Appearance.ApplyNPCHack;
            const string label = "Allow NPC Appearance on Players";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
            using(var combo = ImRaii.Combo(label, allowNPCHackBehavior.ToString()))
            {
                if(combo.Success)
                {
                    foreach(var npcHack in Enum.GetValues<ApplyNPCHack>())
                    {
                        if(ImGui.Selectable($"{npcHack}", npcHack == allowNPCHackBehavior))
                        {
                            _configurationService.Configuration.Appearance.ApplyNPCHack = npcHack;
                            _configurationService.ApplyChange();
                        }
                    }
                }
            }

            bool enableTinting = _configurationService.Configuration.Appearance.EnableTinting;
            if(ImGui.Checkbox("Enable Tinting", ref enableTinting))
            {
                _configurationService.Configuration.Appearance.EnableTinting = enableTinting;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingTab()
    {
        using(var tab = ImRaii.TabItem("Posing"))
        {
            if(tab.Success)
            {
                DrawPosingGeneralSection();
                DrawGPoseSection();
                DrawOverlaySection();
            }
        }
    }

    private void DrawGPoseSection()
    {
        if(ImGui.CollapsingHeader("GPose", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableMouseHook = _configurationService.Configuration.Posing.DisableGPoseMouseSelect;
            if(ImGui.Checkbox("Disable GPose Mouse Select", ref enableMouseHook))
            {
                _configurationService.Configuration.Posing.DisableGPoseMouseSelect = enableMouseHook;
                _configurationService.ApplyChange();
            }

            bool enableBrioTargetChange = _configurationService.Configuration.Posing.BrioTargetChangesWithGPose;
            if(ImGui.Checkbox("Brio Target Changes with GPose Target", ref enableBrioTargetChange))
            {
                _configurationService.Configuration.Posing.BrioTargetChangesWithGPose = enableBrioTargetChange;
                _configurationService.ApplyChange();
            }

            bool enableGPoseTargetChange = _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio;
            if(ImGui.Checkbox("GPose Target Changes with Brio Target", ref enableGPoseTargetChange))
            {
                _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio = enableGPoseTargetChange;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawOverlaySection()
    {
        if(ImGui.CollapsingHeader("Overlay", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool defaultsOn = _configurationService.Configuration.Posing.OverlayDefaultsOn;
            if(ImGui.Checkbox("Overlay Defaults On", ref defaultsOn))
            {
                _configurationService.Configuration.Posing.OverlayDefaultsOn = defaultsOn;
                _configurationService.ApplyChange();
            }

            bool allowGizmoAxisFlip = _configurationService.Configuration.Posing.AllowGizmoAxisFlip;
            if(ImGui.Checkbox("Allow Gizmo Axis Flip", ref allowGizmoAxisFlip))
            {
                _configurationService.Configuration.Posing.AllowGizmoAxisFlip = allowGizmoAxisFlip;
                _configurationService.ApplyChange();
            }

            bool hideGizmoWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen;
            if(ImGui.Checkbox("Hide Gizmo while Advanced Posing", ref hideGizmoWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen = hideGizmoWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("Show Skeleton Lines", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            bool hideSkeletonWhenGizmoActive = _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive;
            if(ImGui.Checkbox("Hide Skeleton when Gizmo Active", ref hideSkeletonWhenGizmoActive))
            {
                _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive = hideSkeletonWhenGizmoActive;
                _configurationService.ApplyChange();
            }

            float lineThickness = _configurationService.Configuration.Posing.SkeletonLineThickness;
            if(ImGui.DragFloat("Line Thickness", ref lineThickness, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.SkeletonLineThickness = lineThickness;
                _configurationService.ApplyChange();
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleSize;
            if(ImGui.DragFloat("Circle Size", ref circleSize, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.BoneCircleSize = circleSize;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingGeneralSection()
    {
        if(ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
            if(ImGui.DragInt("Undo History", ref undoStackSize, 1, 0, 100))
            {
                _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawWorldTab()
    {
        using(var tab = ImRaii.TabItem("World"))
        {
            if(tab.Success)
            {
                DrawEnvironmentSection();
            }
        }
    }

    private void DrawEnvironmentSection()
    {
        if(ImGui.CollapsingHeader("Environment", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var resetTimeOnGPoseExit = _configurationService.Configuration.Environment.ResetTimeOnGPoseExit;
            if(ImGui.Checkbox("Reset Time on GPose Exit", ref resetTimeOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetTimeOnGPoseExit = resetTimeOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWeatherOnGPoseExit = _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit;
            if(ImGui.Checkbox("Reset Weather on GPose Exit", ref resetWeatherOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit = resetWeatherOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWaterOnGPoseExit = _configurationService.Configuration.Environment.ResetWaterOnGPoseExit;
            if(ImGui.Checkbox("Reset Water on GPose Exit", ref resetWaterOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWaterOnGPoseExit = resetWaterOnGPoseExit;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawKeysTab()
    {
        using(var tab = ImRaii.TabItem("Key Binds"))
        {
            if(!tab.Success)
                return;

            if(ImGui.CollapsingHeader("Interface", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var incrementSmallModifierKeyBind = _configurationService.Configuration.Interface.IncrementSmallModifierKeyBind;
                if(Keybinds.KeySelector("Increment Small Modifier", ref incrementSmallModifierKeyBind))
                {
                    _configurationService.Configuration.Interface.IncrementSmallModifierKeyBind = incrementSmallModifierKeyBind;
                    _configurationService.ApplyChange();
                }

                var incrementLargeModifierKeyBind = _configurationService.Configuration.Interface.IncrementLargeModifierKeyBind;
                if(Keybinds.KeySelector("Increment Large Modifier", ref incrementLargeModifierKeyBind))
                {
                    _configurationService.Configuration.Interface.IncrementLargeModifierKeyBind = incrementLargeModifierKeyBind;
                    _configurationService.ApplyChange();
                }
            }

            if(ImGui.CollapsingHeader("Posing", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var disableGizmo = _configurationService.Configuration.Posing.DisableGizmoKeyBind;
                if(Keybinds.KeySelector("Disable Gizmo", ref disableGizmo))
                {
                    _configurationService.Configuration.Posing.DisableGizmoKeyBind = disableGizmo;
                    _configurationService.ApplyChange();
                }

                var disableSkeleton = _configurationService.Configuration.Posing.DisableSkeletonKeyBind;
                if(Keybinds.KeySelector("Disable Skeleton", ref disableSkeleton))
                {
                    _configurationService.Configuration.Posing.DisableSkeletonKeyBind = disableSkeleton;
                    _configurationService.ApplyChange();
                }

                var hideOverlayKeyBind = _configurationService.Configuration.Posing.HideOverlayKeyBind;
                if(Keybinds.KeySelector("Hide Overlay", ref hideOverlayKeyBind))
                {
                    _configurationService.Configuration.Posing.HideOverlayKeyBind = hideOverlayKeyBind;
                    _configurationService.ApplyChange();
                }
            }
        }
    }
}
