using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class ActorAppearanceWindow : Window, IDisposable
{
    private readonly CustomizeEditor _customizeEditor;
    private readonly GearEditor _gearEditor;
    private readonly ExtendedAppearanceEditor _extendedAppearanceEditor;
    private readonly ModelShaderEditor _modelShaderEditor;

    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private ActorAppearanceCapability _capability = null!;
    private AppearanceImportOptions _importOptions = AppearanceImportOptions.Default;

    public ActorAppearanceWindow(EntityManager entityManager, GPoseService gPoseService) : base($"{Brio.Name} - Appearance###brio_character_editor_window")
    {
        Namespace = "brio_character_editor_namespace";


        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _customizeEditor = new();
        _gearEditor = new();
        _extendedAppearanceEditor = new();
        _modelShaderEditor = new();

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(970, 720),
            MaximumSize = new Vector2(8000, 3000)
        };

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    public override bool DrawConditions()
    {
        if(_entityManager.SelectedEntity is ActorEntity actor && actor.IsProp == true)
        {
            return false;
        }

        if(!_entityManager.SelectedHasCapability<ActorAppearanceCapability>())
        {
            return false;
        }

        return base.DrawConditions();
    }


    public unsafe override void Draw()
    {
        if(_entityManager.TryGetCapabilityFromSelectedEntity<ActorAppearanceCapability>(out var capability, considerParents: true))
        {
            _capability = capability;
        }
        else
        {
            return;
        }

        WindowName = $"{Brio.Name} - Appearance - {capability.Entity.FriendlyName}###brio_character_editor_window";

        var currentAppearance = _capability.CurrentAppearance;
        var originalAppearance = _capability.OriginalAppearance;

        float windowWidth = ImGui.GetWindowWidth();
        float customizeWidth = windowWidth * 0.3f;

        bool shouldSetAppearance = false;

        try
        {
            using(var customizeChild = ImRaii.Child("leftpane", new Vector2(customizeWidth, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(customizeChild.Success)
                {
                    shouldSetAppearance |= _customizeEditor.DrawCustomize(ref currentAppearance, originalAppearance, _capability);
                }
            }
        }
        catch(Exception ex) { Brio.Log.Error(ex, "Error drawing customize pane"); }

        ImGui.SameLine();

        using(var gearChild = ImRaii.Child("rightpane", new Vector2(ImGui.GetContentRegionAvail().X, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            float sectionWidth = ImGui.GetContentRegionAvail().X / 3f - ImGui.GetStyle().FramePadding.X;

            if(gearChild.Success)
            {
                shouldSetAppearance |= _gearEditor.DrawGear(ref currentAppearance, originalAppearance, _capability);
            }

            using(var extendedChild = ImRaii.Child("extended", new Vector2(sectionWidth, -1), true))
            {
                if(extendedChild.Success)
                {
                    shouldSetAppearance |= _extendedAppearanceEditor.Draw(ref currentAppearance, originalAppearance, _capability.CanTint);
                }
            }

            ImGui.SameLine();

            using(var shaderChild = ImRaii.Child("shaders", new Vector2(sectionWidth, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(shaderChild.Success)
                {
                    var shaderParams = capability.Character.GetShaderParams();
                    if(shaderParams != null)
                    {
                        shouldSetAppearance |= _modelShaderEditor.Draw(*shaderParams, ref _capability._modelShaderOverride, _capability);
                    }
                }
            }

            ImGui.SameLine();

            using(var optionsChild = ImRaii.Child("options", new Vector2(-1, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(optionsChild.Success)
                {
                    DrawOptions();
                }
            }
        }

        if(shouldSetAppearance)
            _ = capability.SetAppearance(currentAppearance, AppearanceImportOptions.All);

    }

    private void DrawOptions()
    {
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X / 2.0f - ImGui.GetStyle().FramePadding.X, 0);

        using(ImRaii.Disabled(!_capability.IsAppearanceOverridden))
        {
            if(ImGui.Button("Revert", buttonSize))
                _ = _capability.ResetAppearance();
        }

        ImGui.SameLine();

        if(ImGui.Button("Redraw", buttonSize))
            _ = _capability.Redraw();

        if(ImGui.Button("Load NPC", buttonSize))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("window_load_npc");
        }

        ImGui.SameLine();

        if(ImGui.Button("Import", buttonSize))
            FileUIHelpers.ShowImportCharacterModal(_capability, _importOptions);


        using(var importPopup = ImRaii.Popup("window_load_npc"))
        {
            if(importPopup.Success)
            {
                if(AppearanceEditorCommon.DrawNPCSelector(_capability, _importOptions))
                    ImGui.CloseCurrentPopup();
            }
        }

        if(ImGui.Button("Export", buttonSize))
            FileUIHelpers.ShowExportCharacterModal(_capability);

        ImGui.SameLine();

        DrawImportOptions();

        ImGui.Separator();

        AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(_capability);


    }

    private void DrawImportOptions()
    {
        if(ImBrio.FontIconButtonRight("import_options", FontAwesomeIcon.Cog, 1, "Import Options"))
            ImGui.OpenPopup("import_options_popup_appearance");

        using(var popup = ImRaii.Popup("import_options_popup_appearance"))
        {
            if(popup.Success)
            {
                bool customize = _importOptions.HasFlag(AppearanceImportOptions.Customize);
                if(ImGui.Checkbox("Customize", ref customize))
                {
                    if(customize)
                        _importOptions |= AppearanceImportOptions.Customize;
                    else
                        _importOptions &= ~AppearanceImportOptions.Customize;
                }

                bool gear = _importOptions.HasFlag(AppearanceImportOptions.Equipment);
                if(ImGui.Checkbox("Gear", ref gear))
                {
                    if(gear)
                        _importOptions |= AppearanceImportOptions.Equipment;
                    else
                        _importOptions &= ~AppearanceImportOptions.Equipment;
                }

                bool weapons = _importOptions.HasFlag(AppearanceImportOptions.Weapon);
                if(ImGui.Checkbox("Weapons", ref weapons))
                {
                    if(gear)
                        _importOptions |= AppearanceImportOptions.Weapon;
                    else
                        _importOptions &= ~AppearanceImportOptions.Weapon;
                }

                bool extended = _importOptions.HasFlag(AppearanceImportOptions.ExtendedAppearance);
                if(ImGui.Checkbox("Extended", ref extended))
                {
                    if(extended)
                        _importOptions |= AppearanceImportOptions.ExtendedAppearance;
                    else
                        _importOptions &= ~AppearanceImportOptions.ExtendedAppearance;
                }

                bool shaders = _importOptions.HasFlag(AppearanceImportOptions.Shaders);
                if(ImGui.Checkbox("Shaders", ref shaders))
                {
                    if(shaders)
                        _importOptions |= AppearanceImportOptions.Shaders;
                    else
                        _importOptions &= ~AppearanceImportOptions.Shaders;
                }
            }
        }
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(!newState)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
    }
}
