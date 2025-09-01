using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
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
            MinimumSize = new Vector2(500, 610),
            MaximumSize = new Vector2(1200, 1100)
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

    int selected = 0;
    bool isAdvancedMenuOpen = true;
    public unsafe override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<ActorAppearanceCapability>(out ActorAppearanceCapability? capability, considerParents: true))
            return;

        _capability = capability;

        WindowName = $"{Brio.Name} - Appearance - {capability.Entity.FriendlyName}###brio_character_editor_window";

        using(ImRaii.Disabled(capability.Entity.IsLoading))
        {
            DrawHeader();

            DrawBody(capability);
        }

        if(capability.Entity.IsLoading)
        {
            EntityHelpers.DrawSpinner();
        }
    }

    public unsafe void DrawBody(ActorAppearanceCapability? actorAppearance)
    {
        var currentAppearance = _capability.CurrentAppearance;
        var originalAppearance = _capability.OriginalAppearance;

        float windowWidth = ImGui.GetWindowWidth();
        float customizeWidth = windowWidth - 13;

        bool shouldSetAppearance = false;

        ImBrio.ToggleButtonStrip("appearance_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Equipment", "Customize"]);

        if(selected == 1)
        {
            try
            {
                using(var customizeChild = ImRaii.Child("customizePane", new Vector2(ImGui.GetContentRegionAvail().X, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    if(customizeChild.Success)
                    {
                        shouldSetAppearance |= _customizeEditor.DrawCustomize(ref currentAppearance, originalAppearance, _capability);
                    }

                    float sectionWidth = ImGui.GetContentRegionAvail().X / 2f - ImGui.GetStyle().FramePadding.X + 2;

                    using(var extendedChild = ImRaii.Child("extended", new Vector2(sectionWidth, -1), true))
                    {
                        if(extendedChild.Success)
                        {
                            shouldSetAppearance |= _extendedAppearanceEditor.Draw(ref currentAppearance, originalAppearance, _capability.CanTint);
                        }
                    }

                    ImGui.SameLine();

                    var shaderParams = actorAppearance!.Character.GetShaderParams();
                    using(var shaderChild = ImRaii.Child("shaders", new Vector2(sectionWidth, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        if(shaderChild.Success && shaderParams is not null)
                        {
                            shouldSetAppearance |= _modelShaderEditor.Draw(*shaderParams, ref _capability._modelShaderOverride, _capability);
                        }
                    }
                }
            }
            catch(Exception ex) { Brio.Log.Error(ex, "Error drawing customize pane"); }
        }
        else
        {
            using(var gearChild = ImRaii.Child("equipmentPane", new Vector2(ImGui.GetContentRegionAvail().X, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(gearChild.Success)
                {
                    shouldSetAppearance |= _gearEditor.DrawGear(ref currentAppearance, originalAppearance, _capability);
                }
            }
        }

        if(shouldSetAppearance)
        {
            _ = actorAppearance?.SetAppearance(currentAppearance, AppearanceImportOptions.All);
        }
    }

    private void DrawHeader()
    {
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X / 3f - (ImGui.GetStyle().FramePadding.X / 2), 0);

        if(ImBrio.Button("Load NPC", FontAwesomeIcon.PersonArrowDownToLine, buttonSize))
        {
            AppearanceEditorCommon.ResetNPCSelector();
            ImGui.OpenPopup("window_load_npc");
        }

        ImGui.SameLine();

        using(ImRaii.Disabled(!_capability.IsAppearanceOverridden))
        {
            if(ImBrio.Button("Revert", FontAwesomeIcon.RedoAlt, buttonSize))
                _ = _capability.ResetAppearance();
        }

        ImGui.SameLine();

        if(ImBrio.Button("Redraw", FontAwesomeIcon.PaintBrush, buttonSize))
            _ = _capability.Redraw();

        using(ImRaii.Disabled(!_capability.HasPenumbraIntegration))
        {
            if(ImBrio.FontIconButton("toggle_adv_bar", isAdvancedMenuOpen ? FontAwesomeIcon.ArrowUp : FontAwesomeIcon.ArrowDown, "Toggle Advanced Menu"))
            {
                isAdvancedMenuOpen = !isAdvancedMenuOpen;
            }
        }

        ImGui.SameLine();

        using(ImRaii.Disabled(_capability.CanMCDF is false))
        {
            using(ImRaii.Disabled(_capability.IsSelf))
            {
                if(ImBrio.FontIconButton("load_mcdf", FontAwesomeIcon.CloudDownloadAlt, "Load MCDF"))
                {
                    FileUIHelpers.ShowImportMCDFModal(_capability);
                }
                ImGui.SameLine();
            }
            if(_capability.IsSelf)
                ImBrio.AttachToolTip("Can not load a MCDF on your Player Character. Spawn an Actor to load a MCDF.");

            using(ImRaii.Disabled(_capability.HasMCDF))
            {
                if(ImBrio.FontIconButton("save_mcdf", FontAwesomeIcon.CloudUploadAlt, "Save MCDF"))
                {
                    FileUIHelpers.ShowExportMCDFModal(_capability);
                }
                ImGui.SameLine();
            }
            if(_capability.HasMCDF)
                ImBrio.AttachToolTip("Can not save a MCDF of a Actor that has a MCDF loaded. Reset this Actor to save a MCDF.");
        }

        if(ImBrio.FontIconButtonRight("import", FontAwesomeIcon.FileDownload, 2, "Import Character"))
            FileUIHelpers.ShowImportCharacterModal(_capability, _importOptions);

        ImGui.SameLine();

        DrawImportOptions();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("export", FontAwesomeIcon.Save, 3, "Save Character File"))
            FileUIHelpers.ShowExportCharacterModal(_capability);

        ImGui.Separator();

        if(isAdvancedMenuOpen)
        {
            AppearanceEditorCommon.DrawPenumbraCollectionSwitcher(_capability);
            AppearanceEditorCommon.DrawGlamourerDesignSwitcher(_capability);
            AppearanceEditorCommon.DrawCustomizePlusProfileSwitcher(_capability);
        }

        using(var importPopup = ImRaii.Popup("window_load_npc"))
        {
            if(importPopup.Success)
            {
                if(AppearanceEditorCommon.DrawNPCSelector(_capability, _importOptions))
                    ImGui.CloseCurrentPopup();
            }
        }
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

        GC.SuppressFinalize(this);
    }
}
