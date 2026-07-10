using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Interop;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.Posing;
using Brio.Game.Types;
using Brio.Game.World;
using Brio.Input;
using Brio.Library;
using Brio.Library.Filters;
using Brio.Services;
using Brio.Services.Models;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Windows;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using OneOf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.UI.Controls.Stateless;

public class FileUIHelpers
{
    private const float MenuWidth = 180f;

    private static readonly (SceneImportOptions Flag, string Label)[] _importCategories =
        [
            (SceneImportOptions.Actors,         "Actors"),
            (SceneImportOptions.Cameras,        "Cameras"),
            (SceneImportOptions.Lights,         "Lights"),
            (SceneImportOptions.WorldObjects,   "World Objects"),
            (SceneImportOptions.Environment,    "Environment"),
            (SceneImportOptions.Folders,        "Folders"),
        ];
    private static readonly SceneImportOptions _allCategories = _importCategories.Aggregate(default(SceneImportOptions), (a, c) => a | c.Flag);

    public static void DrawImportSettingsPopup(ref SceneImportOptions options, ref bool overrideCurrentScene, ref bool relativeLightPositions, ref bool relativeObjectPositions)
    {
        if(ImBrio.FontIconButton("scene_import_settings", FontAwesomeIcon.Cog, "Import Options"))
            ImGui.OpenPopup("##scene_import_settings");

        using var popup = ImRaii.Popup("##scene_import_settings");
        if(!popup)
            return;

        ImBrio.SeparatorText("Scene");

        ImGui.Checkbox("Override Current Scene###opt_override", ref overrideCurrentScene);

        ImBrio.SeparatorText("Positions");

        ImGui.Checkbox("Relative Light Positions###opt_rel_light", ref relativeLightPositions);
        ImGui.Checkbox("Relative Object Positions###opt_rel_obj", ref relativeObjectPositions);

        ImBrio.SeparatorText("Categories");

        bool all = (options & _allCategories) == _allCategories;
        if(ImGui.Checkbox("All###cat_all", ref all))
            options = all ? options | _allCategories : options & ~_allCategories;

        foreach(var (flag, label) in _importCategories)
        {
            bool on = options.HasFlag(flag);
            if(ImGui.Checkbox($"{label}###cat_{flag}", ref on))
                options = on ? options | flag : options & ~flag;
        }
    }

    public static void DrawProjectPopup(SceneService sceneService, EntityManager entityManager, ProjectWindow projectWindow, AutoSaveService autoSaveService, ProjectSystem projectSystem)
    {
        using var popup = ImRaii.Popup("DrawProjectPopup");
        if(!popup.Success)
            return;

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

            using(ImRaii.Disabled(projectSystem.CurrentProject is null))
                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Save, "Save Scene", buttonSize))
                {
                    projectSystem.SaveProject(projectSystem.CurrentProject!);
                    Brio.NotifyInfo("Scene saved.");
                    ImGui.CloseCurrentPopup();
                }
            if(projectSystem.CurrentProject is null)
                ImBrio.AttachToolTip("No project loaded to save to");
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Save this Scene in the currently loaded Project");

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.FileCirclePlus, "Save as new...", buttonSize))
            {
                ModalManager.Instance.OpenSaveProjectModal();
                ImGui.CloseCurrentPopup();
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Save this Scene as a new Project");

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.FileImport, "Load Scene", buttonSize))
            {
                projectWindow.IsOpen = true;
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Load on to this Scene");

            ImGui.Spacing();
            ImGui.Separator();

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.Clock, "Load Auto-Saves", buttonSize))
            {
                UIManager.Instance.ToggleAutoSaveWindow();
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Load an Auto-Saves on this scene");

            ImBrio.SeparatorText("Export");

            //using(ImRaii.Disabled(projectSystem.CurrentProject is null))
            using(ImRaii.Group())
            using(ImRaii.Disabled(true))
            {
                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Upload, "Export Scene", buttonSize))
                {
                    ModalManager.Instance.OpenExportSceneModal();
                    ImGui.CloseCurrentPopup();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Export this Scene to a file");

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Download, "Import Scene", buttonSize))
                {
                    ModalManager.Instance.OpenImportSceneModal();
                    ImGui.CloseCurrentPopup();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Import a Scene from a file");
            }
            ImBrio.AttachToolTip("Importing/Exporting disabled until 0.8.1");
        }
    }

    // TODO (Ken) make all of the other tings here fallow this same `state` pattern, I like it better  
    public class PresetPopupState
    {
        public string Name = string.Empty;
        public readonly Dictionary<EntityId, bool> Selection = [];
        public Preset? Selected = null;
        public bool GroupInFolder = true;
        public int Mode = 0;
    }

    static readonly PresetPopupState _presetState = new();
    public static void DrawPresetPopup(PresetType kind, Entity entity)
    {
        using var popup = ImRaii.Popup("DrawPresetPopup");
        if(!popup.Success)
            return;

        ImBrio.BlurPopup();

        // I hate this, but I don't know what else to do atm and I want to stop working on this
        Brio.TryGetService<PresetSystem>(out var presetSystem);
        Brio.TryGetService<EntityManager>(out var entityManager);
        Brio.TryGetService<LightingService>(out var lightingService);
        Brio.TryGetService<VirtualCameraManager>(out var virtualCameraManager);

        var state = _presetState;

        var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

        ImBrio.SeparatorText($"Presets - [{entity.FriendlyName}]");

        ImBrio.ButtonSelectorStrip($"preset_mode", new Vector2(buttonSize.X, ImBrio.GetLineHeight()), ref state.Mode, ["Save", "Load"]);

        if(state.Mode == 0)
        {
            using(var child = ImRaii.Child("###preset_save_list"u8, new Vector2(buttonSize.X, 150), true))
            {
                if(child.Success)
                {
                    var entities = kind == PresetType.Light
                        ? entityManager.TryGetAllLights().Cast<Entity>()
                        : entityManager.TryGetAllCameras().Cast<Entity>();

                    foreach(var loadedEntity in entities)
                    {
                        state.Selection.TryAdd(loadedEntity.Id, false);
                        bool isChecked = state.Selection[loadedEntity.Id];

                        if(ImGui.Checkbox($"{loadedEntity.FriendlyName}###preset_{loadedEntity.Id}", ref isChecked))
                            state.Selection[loadedEntity.Id] = isChecked;
                    }
                }
            }

            ImBrio.SeparatorText($"Name");
            ImGui.SetNextItemWidth(buttonSize.X);
            ImGui.InputText($"###preset_name", ref state.Name, 64);

            using(ImRaii.Disabled(string.IsNullOrEmpty(state.Name) || !state.Selection.Values.Any(v => v)))
            {
                if(ImBrio.Button("Save as Preset", FontAwesomeIcon.Save, buttonSize, centerTest: true))
                {
                    if(kind == PresetType.Light)
                    {
                        var entities = entityManager.TryGetAllLights().Where(l => state.Selection.GetValueOrDefault(l.Id)).ToList();
                        presetSystem.SaveLightPreset(state.Name, string.Empty, entities);
                    }
                    else if(kind == PresetType.Camera)
                    {
                        var entities = entityManager.TryGetAllCameras().Where(c => state.Selection.GetValueOrDefault(c.Id)).ToList();
                        presetSystem.SaveCameraPreset(state.Name, string.Empty, entities);
                    }

                    state.Name = string.Empty;
                    state.Selection.Clear();
                }
            }
        }
        else
        {
            using(var child = ImRaii.Child($"###preset_load_list", new Vector2(buttonSize.X, 150), true))
            {
                if(child.Success)
                {
                    foreach(var preset in presetSystem.GetPresets(kind))
                    {
                        bool selected = state.Selected is not null && state.Selected.Equals(preset);

                        if(ImGui.Selectable($"{preset.Name}###preset_{preset.Path}", selected))
                            state.Selected = preset;
                    }
                }
            }

            if(state.Selected is not null && state.Selected.EntryCount > 1)
                ImGui.Checkbox($"Group into a new folder", ref state.GroupInFolder);

            var size = new Vector2(buttonSize.X / 2, 0);
            using(ImRaii.Disabled(state.Selected is null))
            {
                if(state.Mode != 0 && ImBrio.Button("Load", FontAwesomeIcon.FileImport, size, centerTest: true))
                {
                    if(kind == PresetType.Light)
                    {
                        var dtos = presetSystem.LoadLightPreset(state.Selected!);
                        var folder = (dtos.Count > 1 && state.GroupInFolder)
                            ? entityManager.CreateEntityOnEntityContainer<FolderEntity>(state.Selected!.Name) : null;

                        if(entityManager.SelectedEntity is LightEntity selectedLight && dtos.Count > 0)
                        {
                            lightingService?.LoadLightFromDTO(dtos[0], selectedLight);
                            selectedLight.Transform = dtos[0].Transform;
                            selectedLight.FriendlyName = dtos[0].FriendlyName;
                            dtos.RemoveAt(0);

                            if(state.GroupInFolder)
                            {
                                entityManager.MoveEntity(selectedLight, folder!);
                            }
                        }

                        foreach(var dto in dtos)
                        {
                            lightingService?.SpawnFromDTO(dto, null, folder);
                        }
                    }
                    else if(kind == PresetType.Camera)
                    {
                        var dtos = presetSystem.LoadCameraPreset(state.Selected!);
                        var folder = (dtos.Count > 1 && state.GroupInFolder)
                            ? entityManager.CreateEntityOnEntityContainer<FolderEntity>(state.Selected!.Name) : null;

                        if(entityManager.SelectedEntity is CameraEntity selectedCamera && dtos.Count > 0 && dtos[0].Camera is not null)
                        {
                            if(dtos[0].CameraType == selectedCamera.CameraType)
                            {
                                virtualCameraManager?.LoadCameraFromDTO(dtos[0], selectedCamera);
                                dtos.RemoveAt(0);

                                if(state.GroupInFolder)
                                {
                                    entityManager.MoveEntity(selectedCamera, folder!);
                                }
                            }
                        }

                        foreach(var dto in dtos)
                        {
                            var (_, cameraId) = virtualCameraManager!.CreateCamera(dto.CameraType, false, false, dto.Camera);

                            if(folder is not null)
                            {
                                var cameraEntity = entityManager.GetEntity<CameraEntity>(new CameraId(cameraId));
                                if(cameraEntity is not null)
                                    entityManager.MoveEntity(cameraEntity, folder);
                            }
                        }
                    }
                }
                ImGui.SameLine();

                if(ImBrio.HoldButton("preset_delete", "Delete", FontAwesomeIcon.Trash, 1.1f, size, centerTest: true, tooltip: "[HOLD]\nDelete Preset"))
                {
                    presetSystem.DeletePreset(state.Selected!);
                    state.Selected = null;
                }
            }
        }
    }

    //

    private static async Task ResolveSmartImport(PosingCapability capability, OneOf<PoseFile, CMToolPoseFile, PoseData> poseFile)
    {
        if(!smartDefaults)
            return;

        PoseFile pose = poseFile.Match<PoseFile>(p => p,
                                                 p => { isCMP = true; return p.Upgrade(); },
                                                 p => (p as PoseFile)!);

        // Auto model transform
        if(pose.ModelId != 0 && capability.Entity.TryGetCapability<ActorAppearanceCapability>(out var appearanceCapability))
        {
            var actorMeta = appearanceCapability.GetPoseMetaData();
            if(actorMeta.ModelId == 0)
            {
                await appearanceCapability.SetAppearance(ActorAppearance.FromModelChara(pose.ModelId), AppearanceImportOptions.Default);
            }

            return;
        }

        pose.SanitizeBoneNames();

        bool hasFaceBones = false;
        bool hasNonFaceBones = false;
        bool hasDawntrailFaceMarker = false;

        foreach(var boneName in pose.Bones.Keys)
        {
            if(boneName.Equals("j_f_bero_01", StringComparison.OrdinalIgnoreCase))
                hasDawntrailFaceMarker = true;

            if(IsFaceBone(boneName))
                hasFaceBones = true;
            else
                hasNonFaceBones = true;

            if(hasFaceBones && hasNonFaceBones && hasDawntrailFaceMarker)
                break;
        }

        bool expressionOnlyTag = HasAnyTag(pose, "expression-only", "expression_only", "expressiononly", "expression", "facial expression", "facial-expression");
        bool bodyOnlyTag = HasAnyTag(pose, "body-only", "body_only", "bodyonly", "body only");

        // Expression/Body only
        if(expressionOnlyTag || (hasFaceBones && !hasNonFaceBones))
        {
            doExpression = true;
            doBody = false;
        }
        else if(bodyOnlyTag || (hasNonFaceBones && !hasFaceBones))
        {
            doBody = true;
            doExpression = false;
        }

        // Dawntrail expression validity
        if(doExpression)
        {
            bool actorIsDawntrail = capability.SkeletonPosing.CharacterIsDawntrail;
            bool isLikelyDT = hasDawntrailFaceMarker || HasAnyTag(pose, "dawntrail", "dt");

            bool validDawntrailExpressionPose = actorIsDawntrail && isLikelyDT;
            if(!validDawntrailExpressionPose)
            {
                Brio.Log.Warning("Blocked expression import because pose is not DT compatible.");

                Brio.NotifyError("Blocked expression import because pose is not DT compatible.");

                doExpression = false;
            }
        }

        static bool IsFaceBone(string boneName)
        {
            if(string.IsNullOrEmpty(boneName))
                return false;

            if(boneName.Equals("j_kao", StringComparison.OrdinalIgnoreCase))
                return true;

            return boneName.StartsWith("j_f_", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("j_eye", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("j_may", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("j_ago", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("j_lip", StringComparison.OrdinalIgnoreCase)
                || boneName.StartsWith("j_bero", StringComparison.OrdinalIgnoreCase);
        }

        static bool HasAnyTag(PoseData pose, params string[] tagsToMatch)
        {
            if(pose.Tags == null || pose.Tags.Count == 0)
                return false;

            foreach(var tag in pose.Tags)
            {
                var name = tag.Name;
                foreach(var token in tagsToMatch)
                {
                    if(name.Contains(token, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }

    static OneOf<PoseFile, CMToolPoseFile, PoseData>? _lastused = null;
    static OneOf<PoseFile, CMToolPoseFile, PoseData>? _stash = null;

    static bool freezeOnLoad = false;
    static bool smartDefaults = false;

    static bool isCMP = false;
    static bool doTransform = false;
    static bool doExpression = false;
    static bool doBody = false;
    static bool[] _importType = [false, false];

    static TransformComponents? transformComponents = null;
    public static void DrawImportPoseMenuPopup(string tag, PosingCapability? capability, bool showImportOptions = true, OneOf<PoseFile, CMToolPoseFile, PoseData>? importPose = null)
    {
        using var popup = ImRaii.Popup($"DrawImportPoseMenuPopup");
        if(!popup.Success)
            return;

        if(capability is null)
            return;

        ImBrio.BlurPopup();

        isCMP = false;

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            var height = 25 * ImGuiHelpers.GlobalScale;
            var width = 245;

            var butonHeight = 11;
            var buttonwidth = (width / 8) + 4;

            var buttonSize = new Vector2(buttonwidth, butonHeight);

            ImBrio.SeparatorText($"Import Pose [{capability.Entity.FriendlyName}]");

            ImGui.Checkbox("Freeze Actor", ref freezeOnLoad);
            ImBrio.AttachToolTip("Freeze the actor on import");

            ImGui.Checkbox("Smart Import", ref smartDefaults);
            ImBrio.AttachToolTip("""

                Smart Import will adapt the loading process based on the pose being imported.

                For example: 
                - If the pose has a Model-ID, it will automatically transform the model to match.
                - If the pose is taged as Expression/Body only, it will automatically disable the other option.
                - If trying to load the pose as an expression, will automatically determine if the pose was made after Dawntrail and adapt the import process accordingly.
                """);

            ImBrio.SeparatorText("Import Type");

            _importType[0] = doBody;
            _importType[1] = doExpression;
            if(ImBrio.ToggleSelecterStrip("importTypeStrip", new(width, height), ref _importType, ["Body", "Expression"], "Import"))
            {
                doBody = _importType[0];
                doExpression = _importType[1];
            }

            ImBrio.VerticalPadding(4);

            using(ImRaii.Disabled(doExpression || doBody))
            {
                if(ImBrio.Button("Custom Import Options", FontAwesomeIcon.Cog, new(width, height), centerTest: true, tooltip: "Custom Bone Import Options"))
                    ImGui.OpenPopup($"import_{tag}_optionsImportPoseMenuPopup");
            }

            ImBrio.SeparatorText("Transform Options");

            transformComponents ??= capability.PosingService.DefaultImporterOptions.TransformComponents;

            using(ImRaii.Disabled(smartDefaults))
            {
                using(ImRaii.Disabled(doExpression))
                {
                    if(ImBrio.ToggelFontIconButton("ImportPosition", FontAwesomeIcon.ArrowsUpDownLeftRight, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Position), tooltip: "Import Position"))
                    {
                        if(transformComponents.Value.HasFlag(TransformComponents.Position))
                            transformComponents &= ~TransformComponents.Position;
                        else
                            transformComponents |= TransformComponents.Position;
                    }
                    ImGui.SameLine();
                    if(ImBrio.ToggelFontIconButton("ImportRotation", FontAwesomeIcon.ArrowsSpin, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Rotation), tooltip: "Import Rotation"))
                    {
                        if(transformComponents.Value.HasFlag(TransformComponents.Rotation))
                            transformComponents &= ~TransformComponents.Rotation;
                        else
                            transformComponents |= TransformComponents.Rotation;
                    }
                    ImGui.SameLine();
                    if(ImBrio.ToggelFontIconButton("ImportScale", FontAwesomeIcon.ExpandAlt, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Scale), tooltip: "Import Scale"))
                    {
                        if(transformComponents.Value.HasFlag(TransformComponents.Scale))
                            transformComponents &= ~TransformComponents.Scale;
                        else
                            transformComponents |= TransformComponents.Scale;
                    }
                }

                ImGui.SameLine();
                if(ImBrio.ToggelFontIconButton("ImportTransform", FontAwesomeIcon.ArrowsToCircle, buttonSize, doTransform, tooltip: "Import Model Transform"))
                {
                    doTransform = !doTransform;
                }

                if(smartDefaults)
                {
                    transformComponents = null;
                }
            }

            ImBrio.SeparatorText("Import");

            if(importPose is not null)
            {
                if(ImBrio.Button("Apply This Pose", FontAwesomeIcon.PersonRays, new(width, height), centerTest: true, tooltip: "Apply the Selected Pose"))
                {
                    isCMP = importPose.Value.IsT1;
                    _ = ImportPose(capability, importPose.Value, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                if(ImBrio.Button("From File...", FontAwesomeIcon.FileDownload, new(width, height), centerTest: true, tooltip: "Import Pose from File"))
                {
                    ShowImportPoseModal(capability, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                }

                using(ImRaii.Disabled(false))
                    if(ImBrio.Button("From Clipboard", FontAwesomeIcon.Paste, new(width, height), centerTest: true, tooltip: "Import Pose from Clipboard"))
                    {
                        var data = ImGui.GetClipboardText();
                        Clipboard.FromCompressedBase64<PoseFile>(data, out var pose);

                        if(pose is null)
                        {
                            Brio.NotifyError("Failed to import pose from clipboard! Data on clipboard is not a valid pose.");
                            Brio.Log.Error("Failed to import pose from clipboard. Data on clipboard is not a valid pose.");
                            return;
                        }

                        try
                        {
                            _ = ImportPose(capability, pose, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                        }
                        catch(Exception ex)
                        {
                            Brio.NotifyError("Failed to import pose from clipboard!");
                            Brio.Log.Error("Failed to import pose from clipboard", ex);
                        }
                    }

                using(ImRaii.Disabled(_lastused is null))
                    if(ImBrio.Button("Reapply Last Pose", FontAwesomeIcon.PersonWalkingArrowLoopLeft, new(width, height), centerTest: true, tooltip: "Reapply Last Imported Pose"))
                    {
                        _ = ImportPose(capability, _lastused!.Value, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                    }

                using(ImRaii.Disabled(_stash is null))
                    if(ImBrio.Button("Load From Stash", FontAwesomeIcon.Archive, new(width, height), centerTest: true, tooltip: "Load from the Pose Stash"))
                    {
                        _ = ImportPose(capability, _stash!.Value, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                    }

                ImBrio.SeparatorText("Presets");

                if(ImGui.Button("Import A-Pose", new(width, height)))
                {
                    capability.LoadResourcesPose("Data.BrioAPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }

                if(ImGui.Button("Import T-Pose", new(width, height)))
                {
                    capability.LoadResourcesPose("Data.BrioTPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }
            }

            using(var popup2 = ImRaii.Popup($"import_{tag}_optionsImportPoseMenuPopup"))
            {
                if(popup2.Success && showImportOptions && Brio.TryGetService<PosingService>(out var service))
                {
                    PosingEditorCommon.DrawImportOptionEditor(service.DefaultImporterOptions, service, true);
                }
            }
        }
    }

    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null, bool asExpression = false,
        bool asBody = false, TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {
        TypeFilter filter = new("Poses", typeof(CMToolPoseFile), typeof(PoseFile));

        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    isCMP = true;
                    _ = ImportPose(capability, cmPose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
                else if(r is PoseFile pose)
                {
                    _ = ImportPose(capability, pose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
            });
        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    isCMP = true;
                    _ = ImportPose(capability, cmPose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
                else if(r is PoseFile pose)
                {
                    _ = ImportPose(capability, pose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
            });
        }
    }

    private static async Task ImportPose(PosingCapability capability, OneOf<PoseFile, CMToolPoseFile, PoseData> rawPoseFile, PoseImporterOptions? options = null,
        TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {

        if(smartDefaults)
            await ResolveSmartImport(capability, rawPoseFile).WaitAsync(new TimeSpan(0, 0, 30));

        _lastused = rawPoseFile;

        if(isCMP & (doBody || doExpression))
        {
            if(doExpression)
            {
                Brio.NotifyError("CMP poses do not support expression import!");

                if(doBody is false)
                    return;
            }

            capability.ImportPose(rawPoseFile, options: capability.PosingService.DefaultCMPImporterOptions, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: applyModelTransformOverride);

            return;
        }

        if(doBody && doExpression)
        {
            capability.ImportPose(rawPoseFile, options: capability.PosingService.DefaultIPCImporterOptions, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: applyModelTransformOverride);
            return;
        }

        if(doBody)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: false, asBody: true, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
        else if(doExpression)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: true, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: null);
        }
        else
        {
            capability.ImportPose(rawPoseFile, options: options, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
    }

    public static void ShowExportPoseModal(PosingCapability? capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Pose###export_pose", "Pose File (*.pose){.pose}", "brio", ".pose",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".pose"))
                            path += ".pose";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        if(capability is null)
                        {
                            Brio.NotifyError("Failed to export pose!!! Please report this issue.");
                            return;
                        }

                        PoseMetaData? poseMetaData = null;
                        if(capability.Entity.TryGetCapability<ActorAppearanceCapability>(out var appearanceCapability))
                        {
                            poseMetaData = appearanceCapability.GetPoseMetaData();
                        }
                        capability?.SavePoseToPath(path, poseMetaData);
                    }
                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void DrawExportPoseMenuPopup(PosingCapability? capability)
    {
        using var popup = ImRaii.Popup("DrawExportPoseMenuPopup");
        if(!popup.Success)
            return;

        if(capability is null)
            return;

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

            ImBrio.SeparatorText($"Export Pose [{capability.Entity.FriendlyName}]");

            if(ImBrio.Button("Export", FontAwesomeIcon.Save, buttonSize, centerTest: true, tooltip: "Export Pose"))
            {
                ShowExportPoseModal(capability);
                ImGui.CloseCurrentPopup();
            }

            if(ImBrio.Button("With Metadata...", FontAwesomeIcon.FileExport, buttonSize, centerTest: true, tooltip: "Export Pose with Metadata"))
            {
                ShowExportPoseMetadataModal(capability);
                ImGui.CloseCurrentPopup();
            }

            ImBrio.VerticalPadding(1);
            ImBrio.SeparatorText("Copy");
            ImBrio.VerticalPadding(1);

            if(ImBrio.Button("To Clipboard", FontAwesomeIcon.Copy, buttonSize, centerTest: true, tooltip: "Copy Pose to Clipboard"))
            {
                try
                {
                    var pose = capability.ExportPoseAsFileData();
                    var data = Clipboard.ToCompressedBase64(pose, version: 1);

                    ImGui.SetClipboardText(data);
                }
                catch(Exception ex)
                {
                    Brio.NotifyError("Failed to copy pose to clipboard!");

                    Brio.Log.Error("Failed to copy pose to clipboard", ex);
                }

                ImGui.CloseCurrentPopup();
            }

            if(ImBrio.Button("To Stash", FontAwesomeIcon.Archive, buttonSize, centerTest: true, tooltip: "Copy Pose to Stash"))
            {
                _stash = capability.ExportPoseAsFileData();
            }
        }
    }

    public static void ShowExportPoseMetadataModal(PosingCapability? capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Pose###export_pose_metadata", "Pose File (*.pose){.pose}", "brio", ".pose",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".pose"))
                            path += ".pose";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        if(capability is null)
                        {
                            Brio.NotifyError("Failed to export pose!!! Please report this issue.");
                            return;
                        }

                        ModalManager.Instance.OpenExportPoseMetadataModal(capability, path);
                    }
                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportCharacterModal(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        List<Type> types = [typeof(ActorAppearanceUnion), typeof(AnamnesisCharaFile)];

        if(capability.CanMCDF)
            types.Add(typeof(MareCharacterDataFile));

        TypeFilter filter = new TypeFilter("Characters", [.. types]);

        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    if(options.HasFlag(AppearanceImportOptions.Shaders))
                    {
                        BrioHuman.ShaderParams shaderParams = appearanceFile;
                        BrioUtilities.ImportShadersFromFile(ref capability._modelShaderOverride, shaderParams);
                    }
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    _ = capability.LoadMCDF(mareFile.GetPath());
                }
            });

        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    if(options.HasFlag(AppearanceImportOptions.Shaders))
                    {
                        BrioHuman.ShaderParams shaderParams = appearanceFile;
                        BrioUtilities.ImportShadersFromFile(ref capability._modelShaderOverride, shaderParams);
                    }
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    _ = capability.LoadMCDF(mareFile.GetPath());
                }
            });
        }
    }

    public static void ShowExportCharacterModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Character File###export_character_window", "Character File (*.chara){.chara}", "brio", "{.chara}",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".chara"))
                            path += ".chara";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportAppearance(path);
                    }

                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportMCDFModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import MCDF File###import_mcdf_window", "Mare Character Data File (*.mcdf){.mcdf}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         if(directory is not null)
                         {
                             ConfigurationService.Instance.Configuration.LastMCDFPath = directory;
                             ConfigurationService.Instance.Save();
                         }
                         _ = capability.LoadMCDF(path);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.LastMCDFPath, true);
    }

    public static void ShowExportMCDFModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export MCDF File###export_mcdf_window", "Mare Character Data File (*.mcdf){.mcdf}", "mcdf", "{.mcdf}",
                 (success, path) =>
                 {
                     if(success && !path.IsNullOrEmpty())
                     {
                         Brio.Log.Info("Exporting MCDF...");
                         if(!path.EndsWith(".mcdf"))
                             path += ".mcdf";

                         var directory = Path.GetDirectoryName(path);
                         if(directory is not null)
                         {
                             ConfigurationService.Instance.Configuration.MCDF.LastSavedCharaDataLocation = directory;
                             ConfigurationService.Instance.Save();
                         }

                         _ = capability.SaveMcdf(path, string.Empty);
                     }
                 }, ConfigurationService.Instance.Configuration.MCDF.LastSavedCharaDataLocation, true);
    }

    public static void ShowExportSceneModal(SceneService sceneService, string? author, string? description)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Scene File###export_scene_window", "Brio Scene File (*.brioscn){.brioscn}", "brioscn", "{.brioscn}",
            (success, path) =>
            {
                if(success)
                {
                    Brio.Log.Info("Exporting scene...");
                    if(!path.EndsWith(".brioscn"))
                        path += ".brioscn";

                    var directory = Path.GetDirectoryName(path);
                    if(directory is not null)
                    {
                        ConfigurationService.Instance.Configuration.LastScenePath = directory;
                        ConfigurationService.Instance.Save();
                    }

                    BrioScene snapshot = sceneService.CaptureScene();
                    snapshot.Manifest.Author = author;
                    snapshot.Manifest.Description = description;

                    byte[] bytes = sceneService.Serialize(snapshot);
                    File.WriteAllBytes(path, bytes);

                    Brio.Log.Info("Finished exporting scene");
                }
            }, ConfigurationService.Instance.Configuration.LastScenePath, true);
    }

    public static void ShowImportSceneModal(SceneService sceneService, bool destroyAll, bool useRelativeLightPositions, bool useRelativeWorldObjectPositions, SceneImportOptions importOptions)
    {
        List<Type> types = [typeof(SceneFile)];
        TypeFilter filter = new("Scenes", [.. types]);

        LibraryManager.GetWithFilePicker(filter, r =>
        {
            Brio.Log.Verbose("Importing scene...");
            if(r is BrioScene importedScene)
            {
                sceneService.ImportScene(importedScene, destroyAll, useRelativeLightPositions, useRelativeWorldObjectPositions, importOptions);
                Brio.Log.Verbose("Finished imported scene!");
            }
            else
            {
                throw new IOException("The file selected is not a valid scene file");
            }
        }, true);
    }

    public static void ShowImportPreviewImageModal(Action<string> onImageSelected, Action? onDialogClosed = null)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog(
            "Import Image File###import_preview_image_window",
            "Image Files (*.png | *.jpg | *.jpeg){.png,.jpg,.jpeg}",
            (success, paths) =>
            {
                if(success && paths.Count == 1)
                {
                    var path = paths[0];
                    var directory = Path.GetDirectoryName(path);
                    if(directory is not null)
                    {
                        ConfigurationService.Instance.Configuration.LastPreviewImagePath = directory;
                        ConfigurationService.Instance.Save();
                    }
                    onImageSelected(path);
                }

                onDialogClosed?.Invoke();
            },
            1,
            ConfigurationService.Instance.Configuration.LastPreviewImagePath,
            true);
    }
}


