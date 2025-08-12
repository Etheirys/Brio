using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class PosingGraphicalWindow : Window, IDisposable
{
    private const float RightPanelWidth = 250;

    private readonly GraphicalPosePositionFile _posePositions;
    private readonly EntityManager _entityManager;
    private readonly CameraService _cameraService;
    private readonly ConfigurationService _configurationService;
    private readonly PosingService _posingService;
    private readonly GPoseService _gPoseService;
    private readonly PhysicsService _physicsService;
    private readonly PosingTransformEditor _transformEditor = new();
    private readonly BoneSearchControl _boneSearchControl = new();
    private float _closestHover = float.MaxValue;

    private Matrix4x4? _trackingMatrix;

    int _selectedPane = 0;
    private bool _hideControlPane = false;

    public PosingGraphicalWindow(EntityManager entityManager, CameraService cameraService, PhysicsService physicsService, ConfigurationService configurationService, PosingService posingService, GPoseService gPoseService) : base($"{Brio.Name} - Posing###brio_posing_graphical_window")
    {
        Namespace = "brio_posing_graphical_namespace";

        _entityManager = entityManager;
        _cameraService = cameraService;
        _configurationService = configurationService;
        _posingService = posingService;
        _gPoseService = gPoseService;
        _physicsService = physicsService;

        _posePositions = ResourceProvider.Instance.GetResourceDocument<GraphicalPosePositionFile>("Data.GraphicalBonePosePositions.json");
        _posePositions.Process();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    public override bool DrawConditions()
    {
        if(_entityManager.SelectedEntity is ActorEntity actor && actor.IsProp == true)
        {
            return false;
        }

        if(!_entityManager.SelectedHasCapability<PosingCapability>() || !_entityManager.SelectedHasCapability<ActorAppearanceCapability>())
        {
            return false;
        }

        return base.DrawConditions();
    }

    public unsafe override void PreDraw()
    {
        ImGui.SetNextWindowSize(new Vector2(1200, 600), ImGuiCond.FirstUseEver);

        ImGui.SetNextWindowSizeConstraints(new Vector2(900, 450), new Vector2(4800, 2400), (x) =>
        {
            x->DesiredSize.X = MathF.Max(x->DesiredSize.X, x->DesiredSize.Y);
            x->DesiredSize.Y = x->DesiredSize.X * 0.5f;
        });

        base.PreDraw();
    }

    public override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<ActorAppearanceCapability>(out var appearance))
        {
            return;
        }

        posing.Hover = new None();
        _closestHover = float.MaxValue;

        WindowName = $"{Brio.Name} - Posing - {posing.Entity.FriendlyName}###brio_posing_graphical_window";

        DrawGlobalButtons(posing);

        //

        float leftPanelWidth;
        if(posing.TransformWindowOpen || _hideControlPane)
            leftPanelWidth = ImBrio.GetRemainingWidth() - ImGui.GetStyle().ItemSpacing.X;
        else
            leftPanelWidth = ImBrio.GetRemainingWidth() - RightPanelWidth - ImGui.GetStyle().ItemSpacing.X;

        using(var child = ImRaii.Child("###left_pane", new Vector2(leftPanelWidth, -1), true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success)
            {
                DrawGraphics(posing, appearance);
            }
        }

        ImGui.SameLine();

        using(var rightPane = ImRaii.Child("###right_pane", new Vector2(RightPanelWidth, -1), false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground))
        {
            if(rightPane.Success && posing.TransformWindowOpen is false && _hideControlPane is false)
            {
                PosingEditorCommon.DrawSelectionName(posing);

                DrawButtons(posing);
                ImGui.Separator();

                float height = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - (ImGui.GetStyle().FramePadding.Y * 2);

                using(var rightPaneSelection = ImRaii.Child("###right_pane_selection", new Vector2(-1, height), true, ImGuiWindowFlags.AlwaysVerticalScrollbar))
                {
                    if(rightPaneSelection.Success)
                    {
                        DrawSelection(posing);
                    }
                }

                DrawImportButtons(posing);
            }
        }

        posing.LastHover = posing.Hover;
    }

    private readonly string[] _bonePages = ["Body Page", "Face Page"];
    private void DrawGlobalButtons(PosingCapability posing)
    {
        const float buttonWidth = 28;

        if(ImBrio.ToggelButton("Freeze Physics", new Vector2(95, 0), _physicsService.IsFreezeEnabled, hoverText: _physicsService.IsFreezeEnabled ? "Un-Freeze Physics" : "Freeze Physics"))
        {
            _physicsService.FreezeToggle();
        }

        ImGui.SameLine();

        if(_entityManager.TryGetCapabilityFromSelectedEntity<ActionTimelineCapability>(out var capability, considerParents: true))
        {
            if(ImBrio.ToggelButton("Freeze Character", new Vector2(110, 0), capability.SpeedMultiplier == 0, hoverText: capability.SpeedMultiplierOverride == 0 ? "Un-Freeze Character" : "Freeze Character"))
            {
                if(capability.SpeedMultiplierOverride == 0)
                    capability.ResetOverallSpeedOverride();
                else
                    capability.SetOverallSpeedOverride(0f);
            }
        }

        ImGui.SameLine();

        ImBrio.ToggleButtonStrip("posing_page_selector", new(ImBrio.GetRemainingWidth() - RightPanelWidth - 10, ImBrio.GetLineHeight()), ref _selectedPane, _bonePages);

        ImGui.SameLine();

        ImBrio.RightAlign(buttonWidth, 8);

        if(ImBrio.FontIconButton((posing.OverlayOpen ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye), new(buttonWidth, 0)))
            posing.OverlayOpen = !posing.OverlayOpen;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(posing.OverlayOpen ? "Close Overlay" : "Show Overlay");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, posing.TransformWindowOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.LocationCrosshairs, new(buttonWidth, 0)))
                posing.TransformWindowOpen = !posing.TransformWindowOpen;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(posing.TransformWindowOpen ? "Close Transform Window" : "Show Transform Window");

        ImGui.SameLine();

        if(ImBrio.FontIconButton(FontAwesomeIcon.Search, new(buttonWidth, 0)))
            ImGui.OpenPopup("graphic_bone_search_popup");

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Bone Search");

        using(var popup = ImRaii.Popup("graphic_bone_search_popup"))
        {
            if(popup.Success)
            {
                _boneSearchControl.Draw("graphic_bone_search", posing);
            }
        }

        ImGui.SameLine();


        ImBrio.RightAlign(buttonWidth, 4);

        using(ImRaii.Disabled(!posing.HasUndoStack))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.Backward, new(buttonWidth, 0)))
                posing.Undo();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo");

        ImGui.SameLine();

        using(ImRaii.Disabled(!posing.HasRedoStack))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.Forward, new(buttonWidth, 0)))
                posing.Redo();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Redo");

        ImGui.SameLine();


        using(ImRaii.Disabled(!posing.HasOverride))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.Undo, new(buttonWidth, 0)))
                posing.Reset(false, false);
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Reset Pose");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _hideControlPane ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            if(ImBrio.FontIconButton(_hideControlPane ? FontAwesomeIcon.SlidersH : FontAwesomeIcon.SlidersH, new(buttonWidth, 0)))
                _hideControlPane = !_hideControlPane;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_hideControlPane ? "Show Control Pane" : "Hide Control Pane");
    }

    private void DrawSelection(PosingCapability posing)
    {
        DrawGizmo();
        ImGui.Separator();
        _transformEditor.Draw("graphical_transform", posing);
    }

    private static void DrawButtons(PosingCapability posing)
    {
        float buttonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * 3f)) / 4f;

        // Mirror mode
        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonWidth, 0));

        // IK
        ImGui.SameLine();
        PosingEditorCommon.DrawIKSelect(posing, new Vector2(buttonWidth, 0));

        // Select Parent
        ImGui.SameLine();
        var parentBone = posing.Selected.Match(
               boneSelect => posing.SkeletonPosing.GetBone(boneSelect)?.GetFirstVisibleParent(),
               _ => null,
               _ => null
        );

        using(ImRaii.Disabled(parentBone == null))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt, new Vector2(buttonWidth, 0)))
                posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Select Parent");

        // Clear Selection
        ImGui.SameLine();
        using(ImRaii.Disabled(posing.Selected.Value is None))
        {
            if(ImGui.Button($"Clear###clear_selected", new Vector2(buttonWidth, 0)))
                posing.ClearSelection();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear Selection");
    }

    private void DrawImportButtons(PosingCapability posing)
    {
        float settingsSize = 28;
        var buttonSize = new Vector2(((ImGui.GetContentRegionAvail().X - settingsSize) / 2.0f) - (ImGui.GetStyle().FramePadding.X * 2), 0);

        if(ImBrio.Button("Import##import_pose", FontAwesomeIcon.FileImport, buttonSize))
            ImGui.OpenPopup("DrawImportPoseMenuPopup");

        FileUIHelpers.DrawImportPoseMenuPopup(posing, false);

        ImGui.SameLine();

        if(ImBrio.FontIconButton(FontAwesomeIcon.Cog, new(settingsSize, 0)))
            ImGui.OpenPopup("import_options_popup_posing_graphical");

        ImGui.SameLine();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Import Options");

        using(var popup = ImRaii.Popup("import_options_popup_posing_graphical"))
        {
            if(popup.Success)
            {
                PosingEditorCommon.DrawImportOptionEditor(_posingService.DefaultImporterOptions);
            }
        }

        if(ImBrio.Button("Export##export_pose", FontAwesomeIcon.FileExport, buttonSize))
            FileUIHelpers.ShowExportPoseModal(posing);
    }

    private unsafe void DrawGizmo()
    {
        var selectedEntity = _entityManager.SelectedEntity;

        if(selectedEntity == null)
            return;

        if(!selectedEntity.TryGetCapability<PosingCapability>(out var posing))
            return;

        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        var selected = posing.Selected;

        var currentTransform = posing.ModelPosing.Transform;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        Matrix4x4? targetMatrix = selected.Match<Matrix4x4?>(
            (boneSelect) =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone == null)
                    return null;

                if(!bone.Skeleton.IsValid)
                    return null;

                if(bone.IsHidden)
                    return null;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                    return null;

                selectedBone = bone;
                return bone.LastTransform.ToMatrix() * new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();
            },
            _ => posing.ModelPosing.Transform.ToMatrix(),
            _ => posing.ModelPosing.Transform.ToMatrix()
        );

        if(targetMatrix == null)
            return;

        var matrix = _trackingMatrix ?? targetMatrix.Value;
        var originalMatrix = matrix;


        if(ImBrio.FontIconButton((_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom)))
            _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.World ? "Switch to Local" : "Switch to World");


        Vector2 gizmoSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X);

        if(ImBrioGizmo.DrawRotation(ref matrix, gizmoSize, _posingService.CoordinateMode == PosingCoordinateMode.World))
        {
            if(!posing.ModelPosing.Freeze && !(selectedBone != null && selectedBone.Freeze))
                _trackingMatrix = matrix;
        }

        if(_trackingMatrix.HasValue)
        {
            selected.Switch(
                boneSelect => posing.SkeletonPosing.GetBonePose(boneSelect).Apply(_trackingMatrix.Value.ToTransform(), originalMatrix.ToTransform()),
                _ => posing.ModelPosing.Transform += _trackingMatrix.Value.ToTransform().CalculateDiff(originalMatrix.ToTransform()),
                _ => posing.ModelPosing.Transform += _trackingMatrix.Value.ToTransform().CalculateDiff(originalMatrix.ToTransform())
            );
        }

        if(!ImBrioGizmo.IsUsing() && _trackingMatrix.HasValue)
        {
            posing.Snapshot(false, false);
            _trackingMatrix = null;
        }
    }

    private void DrawGraphics(PosingCapability posing, ActorAppearanceCapability appearance)
    {
        var currentAppearance = appearance.CurrentAppearance;

        if(appearance.IsHuman is false)
        {
            ImGui.Text("Graphical posing is only available for humanoid characters.");
            if(ImGui.Button("Make Human"))
                appearance.MakeHuman();

            return;
        }

        var contentArea = ImGui.GetContentRegionAvail();
        var contentWidth = contentArea.X / 3f;

        float headerYOffset = 0;

        using(var child = ImRaii.Child("###body_pane", new Vector2(contentWidth, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success)
            {
                Vector2 opening = ImGui.GetCursorPos();

                if(posing.SkeletonPosing.CharacterIsIVCS)
                {
                    bool showGenitalia = _configurationService.Configuration.Posing.ShowGenitaliaInAdvancedPoseWindow;
                    if(ImGui.Checkbox("Show Genitalia", ref showGenitalia))
                    {
                        _configurationService.Configuration.Posing.ShowGenitaliaInAdvancedPoseWindow = showGenitalia;
                    }

                    headerYOffset += 10;
                }
                var swapped = _configurationService.Configuration.Posing.GraphicalSidesSwapped;
                if(ImGui.Checkbox("Swap", ref swapped))
                {
                    _configurationService.Configuration.Posing.GraphicalSidesSwapped = swapped;
                }

                //

                if(_selectedPane == 0)
                {
                    ImGui.SetCursorPos(opening + new Vector2(0, headerYOffset));

                    DrawBoneSection("body", true, posing);
                }
            }
        }

        if(_selectedPane == 0)
        {
            ImGui.SameLine();

            using(var child = ImRaii.Child("###armor_pane", new Vector2(contentWidth, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success)
                {
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, headerYOffset));

                    DrawBoneSection("armor", true, posing);
                }
            }

            ImGui.SameLine();

            using(var child = ImRaii.Child("###details_pane", new Vector2(contentWidth, -1)))
            {
                if(child.Success)
                {
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, headerYOffset));

                    using(var splitChild = ImRaii.Child("###split_details_hands", new Vector2(contentWidth - ImGui.GetStyle().FramePadding.X, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        if(splitChild.Success)
                        {
                            DrawBoneSection("hands", true, posing);

                            if(posing.SkeletonPosing.CharacterIsIVCS)
                            {
                                DrawBoneSection("ivcs_toes", true, posing);
                            }

                            if(posing.SkeletonPosing.CharacterHasTail)
                            {
                                DrawBoneSection("tail", false, posing);
                            }

                            if(posing.SkeletonPosing.CharacterIsIVCS && _configurationService.Configuration.Posing.ShowGenitaliaInAdvancedPoseWindow)
                            {
                                ImGui.SameLine();

                                DrawBoneSection("ivcs", false, posing);
                            }
                        }
                    }
                }
            }
        }

        if(_selectedPane == 1)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, headerYOffset));

            ImGui.SameLine();

            using(var child = ImRaii.Child("###face_pane", new Vector2(contentArea.X - 35, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success)
                {
                    switch(currentAppearance.Customize.Race)
                    {
                        case Races.Hyur:
                        case Races.Elezen:
                        case Races.Roegadyn:
                        case Races.Lalafel:
                        case Races.AuRa:
                            DrawBoneSection("human_head", true, posing);
                            break;

                        case Races.Miqote:
                            DrawBoneSection("miqote_head", true, posing);
                            break;

                        case Races.Hrothgar:
                            DrawBoneSection("hrothgar_head", true, posing);
                            break;

                        case Races.Viera:
                            switch(currentAppearance.Customize.RaceFeatureType)
                            {
                                case 1:
                                    DrawBoneSection("viera_head_a", true, posing);
                                    break;
                                case 2:
                                    DrawBoneSection("viera_head_b", true, posing);
                                    break;
                                case 3:
                                    DrawBoneSection("viera_head_c", true, posing);
                                    break;
                                case 4:
                                    DrawBoneSection("viera_head_d", true, posing);
                                    break;
                                default:
                                    DrawBoneSection("viera_head_a", true, posing);
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        // Check if the user has clicked on the background to clear selection.
        Vector2 mousePos = ImGui.GetMousePos() - ImGui.GetWindowPos();
        bool isMouseOverArea = (mousePos.X > 0 && mousePos.Y > 0 && mousePos.X < contentArea.X && mousePos.Y < contentArea.Y);
        if(ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsAnyItemHovered() && isMouseOverArea && posing.LastHover.IsT2)
        {
            posing.ClearSelection();
        }
    }

    private void DrawBoneSection(string sectionName, bool drawMirrors, PosingCapability posing)
    {
        var section = _posePositions.PoseImages[sectionName];
        var position = ImGui.GetCursorPos();

        Vector2 imageSize = new(1024, 2048);
        Vector2 scalingFactors = new(0.2f, 0.2f);

        if(!string.IsNullOrEmpty(section.Image))
            DrawImage($"Images.{section.Image}.png", out imageSize, out scalingFactors);

        var endPosition = ImGui.GetCursorPos();

        var drawBones = new List<DrawBoneEntry>();

        foreach(var graphicBone in section.Bones)
        {
            PosingSelectionType selectionType;

            if(graphicBone.Name == "!model")
                selectionType = PosingSelectionType.ModelTransform;
            else
            {
                var bestBone = posing.SkeletonPosing.GetBone(graphicBone.Name, PoseInfoSlot.Character);
                if(bestBone == null)
                    continue;

                selectionType = new BonePoseInfoId(bestBone.Name, bestBone.PartialId, PoseInfoSlot.Character);
            }

            var swapSides = _configurationService.Configuration.Posing.GraphicalSidesSwapped;

            var transformedPosition = position + (graphicBone.Position * scalingFactors);
            if(swapSides && drawMirrors)
                transformedPosition.X = imageSize.X - transformedPosition.X;

            drawBones.Add(new DrawBoneEntry(selectionType, transformedPosition, scalingFactors.X));

            if(drawMirrors)
            {
                if(selectionType.Value is BonePoseInfoId boneInfo)
                {
                    var mirror = boneInfo.GetMirrorBone();
                    if(mirror != null)
                    {
                        var mirrortransformedPosition = position + (graphicBone.Position * scalingFactors);
                        if(!swapSides)
                            mirrortransformedPosition.X = imageSize.X - mirrortransformedPosition.X;
                        drawBones.Add(new DrawBoneEntry(mirror.Value, mirrortransformedPosition, scalingFactors.X));
                    }
                }
            }
        }

        foreach(var entry in drawBones)
        {
            DrawBone(entry, drawBones, posing);
        }

        ImGui.SetCursorPos(endPosition);
    }

    private void DrawBone(DrawBoneEntry entry, IReadOnlyList<DrawBoneEntry> entries, PosingCapability posing)
    {
        bool enabled = false;
        bool selected = posing.Selected == entry.Id;
        bool hovered = posing.LastHover == entry.Id;
        Vector2? parentPosition = null;
        bool branchHovered = false;
        bool branchSelected = false;

        bool anyBoneSelected = posing.Selected.IsT0;
        bool anyBoneHovered = posing.LastHover.IsT0;

        entry.Id.Switch(
            boneSelect =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone != null)
                {
                    enabled = true;

                    if(bone.Parent != null)
                    {
                        var parentId = new BonePoseInfoId(bone.Parent.Name, bone.Parent.PartialId, PoseInfoSlot.Character);
                        var parent = entries.FirstOrDefault(x => x.Id.Value.Equals(parentId));
                        if(parent != null)
                            parentPosition = parent.Position;

                        // test skeleton branch selection
                        if(anyBoneSelected || anyBoneHovered)
                        {
                            var branchParent = bone.Parent;
                            while(branchParent != null)
                            {
                                var branchParentId = new BonePoseInfoId(branchParent.Name, branchParent.PartialId, PoseInfoSlot.Character);
                                if(posing.Selected.IsT0 && posing.Selected.AsT0 == branchParentId)
                                {
                                    branchSelected = true;
                                }

                                if(posing.LastHover.IsT0 && posing.LastHover.AsT0 == branchParentId)
                                {
                                    branchHovered = true;
                                }

                                branchParent = branchParent.Parent;
                            }
                        }
                    }
                }
            },
            _ =>
            {
                enabled = true;
            },
            _ => { }
        );

        float circleSize = 6;
        float hitSize = circleSize + 12;

        using(ImRaii.Disabled(!enabled))
        {
            ImGui.SetCursorPos(entry.Position - new Vector2(ImGui.GetFrameHeight() / 2));
            Vector2 pos = ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetFrameHeight() / 2);

            float mouseDistance = Vector2.Distance(ImGui.GetMousePos(), pos);
            if(mouseDistance < hitSize && mouseDistance < _closestHover)
            {
                _closestHover = mouseDistance;
                posing.Hover = entry.Id;
            }

            uint lineCol = ImGui.GetColorU32(ImGuiCol.TextDisabled);
            if(branchSelected)
            {
                lineCol = ImGui.GetColorU32(ImGuiCol.CheckMark);
            }
            if(branchHovered)
            {
                lineCol = ImGui.GetColorU32(ImGuiCol.Text);
            }

            uint circleColor = lineCol;
            if(selected)
            {
                circleColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
            }
            if(hovered)
            {
                circleColor = ImGui.GetColorU32(ImGuiCol.Text);
            }

            if(parentPosition != null)
            {
                Vector2 parentPos = ImGui.GetCursorScreenPos() + (parentPosition.Value - entry.Position) + new Vector2(ImGui.GetFrameHeight() / 2);
                Vector2 offset = Vector2.Normalize(parentPos - pos) * (circleSize - 0.5f);

                ImGui.GetWindowDrawList().AddLine(pos + offset, parentPos - offset, lineCol, 1);
            }

            ImGui.GetWindowDrawList().AddCircleFilled(
                pos,
                circleSize,
                ImGui.GetColorU32(ImGuiCol.ChildBg));

            ImGui.GetWindowDrawList().AddCircle(pos, circleSize, circleColor);

            if(hovered || selected)
            {
                ImGui.GetWindowDrawList().AddCircleFilled(
                    pos,
                    circleSize - 3,
                    selected ? ImGui.GetColorU32(ImGuiCol.CheckMark) : ImGui.GetColorU32(ImGuiCol.TextDisabled));
            }

            if(hovered && ImGui.IsWindowHovered())
            {
                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    posing.Selected = entry.Id;
                }

                ImGui.SetTooltip(entry.Id.DisplayName);
            }
        }
    }

    private static void DrawImage(string image, out Vector2 imageSizeToFit, out Vector2 scalingFactors)
    {
        var img = ResourceProvider.Instance.GetResourceImage(image) ?? throw new NullReferenceException("image can not be null!");

        var available = ImGui.GetContentRegionAvail() - (ImGui.GetStyle().FramePadding * 2f);
        var imageSize = new Vector2(img.Width, img.Height);
        var aspectRatio = imageSize.X / imageSize.Y;
        imageSizeToFit = new Vector2(available.X, available.X / aspectRatio);
        if(imageSizeToFit.Y > available.Y)
        {
            imageSizeToFit = new Vector2(available.Y * aspectRatio, available.Y);
        }

        ImGui.Image(img.Handle, imageSizeToFit);

        var scaleX = imageSizeToFit.X / imageSize.X;
        var scaleY = imageSizeToFit.Y / imageSize.Y;

        scalingFactors = new Vector2(scaleX, scaleY);
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(newState is false)
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
    }

    private record class DrawBoneEntry(PosingSelectionType Id, Vector2 Position, float Scale);

    private class GraphicalPosePositionFile
    {
        public Dictionary<string, PoseImageContainer> PoseImages { get; set; } = [];

        public record class PoseImageContainer(string Image, string? Parent, List<PoseImageEntry> Bones);
        public record class PoseImageEntry(string Name, Vector2 Position);

        public void Process()
        {
            foreach(var entry in PoseImages.Values)
            {
                if(entry.Parent == null)
                    continue;

                var parent = PoseImages[entry.Parent];
                entry.Bones.AddRange(parent.Bones);
            }
        }
    }
}
