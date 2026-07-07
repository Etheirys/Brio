using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.Posing;
using Brio.Services;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class PosingTransformWindow : Window
{
    private readonly EntityManager _entityManager;
    private readonly PosingService _posingService;
    private readonly CameraService _cameraService;
    private readonly PosingTransformEditor _posingTransformEditor = new();
    private readonly ITransformableEditor _transformableEditor = new();

    private Matrix4x4? _trackingMatrix;

    public PosingTransformWindow(EntityManager entityManager, CameraService cameraService, PosingService posingService) : base($"{Brio.Name} - TRANSFORM###brio_transform_window", ImGuiWindowFlags.AlwaysVerticalScrollbar)
    {
        Namespace = "brio_transform_namespace";

        _entityManager = entityManager;
        _cameraService = cameraService;
        _posingService = posingService;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(350, 850),
            MinimumSize = new Vector2(200, 150)
        };

        this.AllowBackgroundBlur = false;
    }

    public override bool DrawConditions()
    {
        if(!(_entityManager.SelectedEntity is TransformableEntity transformableEntity))
            return false;

        return base.DrawConditions();
    }

    public unsafe override void Draw()
    {
        ImBrio.BlurWindow();

        if(!(_entityManager.SelectedEntity is TransformableEntity transformableEntity))
            return;

        if(_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            WindowName = $"TRANSFORM - {posing.Entity.FriendlyName}###brio_transform_window";

            PosingEditorCommon.DrawSelectionName(posing);

            DrawButtons(posing);
            ImGui.Separator();
            DrawGizmo(posing);
            ImGui.Separator();

            _posingTransformEditor.Draw("overlay_transforms_edit", posing);
        }
        else
        {
            WindowName = $"TRANSFORM - {transformableEntity.FriendlyName}###brio_transform_window";

            DrawGizmo(null);
            ImGui.Separator();

            _transformableEditor.Draw("overlay_transforms_edit", transformableEntity, 0.1f);
        }
    }

    private static void DrawButtons(PosingCapability posing)
    {
        float buttonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * 3f)) / 4f;

        // IK
        PosingEditorCommon.DrawIKSelect(posing, new Vector2(buttonWidth, 0));
        ImGui.SameLine();

        // Clear Selection
        ImGui.SameLine();
        using(ImRaii.Disabled(posing.Selected.Value is None))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.MinusSquare, new Vector2(buttonWidth, 0)))
                posing.ClearSelection();
        }
        ImBrio.AttachToolTip("Clear Selection");

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
                posing.SetBoneSelection(new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character), false);
        }
        ImBrio.AttachToolTip("Select Parent");

        ImGui.SameLine();
        // Mirror mode
        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonWidth, 0));

    }

    private unsafe void DrawGizmo(PosingCapability? posing)
    {
        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        var allTransformables = _entityManager.GetAllSelectedTransformables();
        if(allTransformables.Count == 0)
            return;

        bool isMultiEntitySelection = allTransformables.Count > 1;
        Vector3? multiEntityCentroid = isMultiEntitySelection
            ? TransformHelper.GetCentroidForGivenTransforms(allTransformables.Select(x => x.target.Transform))
            : null;

        var selected = posing?.Selected;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        Matrix4x4? targetMatrix = posing is not null
            ? selected!.Match<Matrix4x4?>(
                (boneSelect) =>
                {
                    if(isMultiEntitySelection)
                        return null;

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
                _ => isMultiEntitySelection ?
                    new Transform { Position = multiEntityCentroid!.Value, Rotation = Quaternion.Identity, Scale = Vector3.One }.ToMatrix() :
                    posing.ModelPosing.Transform.ToMatrix(),
                _ => isMultiEntitySelection ?
                    new Transform { Position = multiEntityCentroid!.Value, Rotation = Quaternion.Identity, Scale = Vector3.One }.ToMatrix() :
                    posing.ModelPosing.Transform.ToMatrix()
            )
            : isMultiEntitySelection ?
                new Transform { Position = multiEntityCentroid!.Value, Rotation = Quaternion.Identity, Scale = Vector3.One }.ToMatrix() :
                allTransformables[0].target.Transform.ToMatrix();

        if(targetMatrix == null)
            return;

        var matrix = _trackingMatrix ?? targetMatrix.Value;
        var originalMatrix = matrix;


        if(ImBrio.FontIconButton((_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom)))
            _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.World ? "Switch to Local" : "Switch to World");

        if(isMultiEntitySelection)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1), $"({allTransformables.Count} entities)");
        }

        Vector2 gizmoSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X);

        if(ImBrioGizmo.DrawRotation(ref matrix, gizmoSize, _posingService.CoordinateMode == PosingCoordinateMode.World))
        {
            bool canEdit;

            if(isMultiEntitySelection)
                canEdit = allTransformables.Any(x => !x.target.IsTransformFrozen);
            else if(posing is not null)
                canEdit = !posing.ModelPosing.IsTransformFrozen && !(selectedBone != null && selectedBone.Freeze);
            else
                canEdit = !allTransformables[0].target.IsTransformFrozen;

            if(canEdit)
            {
                _trackingMatrix = matrix;
            }
        }

        if(_trackingMatrix.HasValue)
        {
            var delta = _trackingMatrix.Value.ToTransform().CalculateDiff(originalMatrix.ToTransform());

            if(isMultiEntitySelection)
            {
                TransformHelper.ApplyDeltaToMultiple(allTransformables, delta, multiEntityCentroid!.Value, true);
            }
            else if(posing is not null)
            {
                selected!.Switch(
                    boneSelect =>
                    {
                        if(posing.IsMultiSelecting)
                        {
                            foreach(var selectedBoneId in posing.SelectedBones)
                            {
                                var targetBone = posing.SkeletonPosing.GetBone(selectedBoneId);
                                if(targetBone != null && !targetBone.Freeze)
                                {
                                    var bonePose = posing.SkeletonPosing.GetBonePose(selectedBoneId);
                                    var boneTransform = targetBone.LastTransform;
                                    bonePose.Apply(boneTransform + delta, boneTransform);
                                }
                            }
                        }
                        else
                        {
                            posing.SkeletonPosing.GetBonePose(boneSelect).Apply(_trackingMatrix.Value.ToTransform(), originalMatrix.ToTransform());
                        }
                    },
                    _ => TransformHelper.ApplyDelta(posing.Actor, delta),
                    _ => TransformHelper.ApplyDelta(posing.Actor, delta)
                );
            }
            else
            {
                TransformHelper.ApplyDelta(allTransformables[0].target, delta);
            }
        }


        if(!ImBrioGizmo.IsUsing() && _trackingMatrix.HasValue)
        {
            TransformHelper.SnapshotAll(_entityManager.GetAllSelectedTransformables().Select(x => x.target));

            _trackingMatrix = null;
        }
    }
}

