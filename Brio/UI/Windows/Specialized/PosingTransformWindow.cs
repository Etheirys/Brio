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
    private readonly HistoryService _historyService;
    private readonly PosingTransformEditor _posingTransformEditor = new();

    private Matrix4x4? _trackingMatrix;
    private List<(EntityId id, PoseInfo info, Transform model)>? _groupedPendingSnapshot = null;

    public PosingTransformWindow(EntityManager entityManager, CameraService cameraService, PosingService posingService, HistoryService historyService) : base($"{Brio.Name} - TRANSFORM###brio_transform_window", ImGuiWindowFlags.AlwaysVerticalScrollbar)
    {
        Namespace = "brio_transform_namespace";

        _entityManager = entityManager;
        _cameraService = cameraService;
        _posingService = posingService;
        _historyService = historyService;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(350, 850),
            MinimumSize = new Vector2(200, 150)
        };
    }

    public override bool DrawConditions()
    {
        if(!_entityManager.SelectedHasCapability<PosingCapability>())
            return false;

        return base.DrawConditions();
    }

    public unsafe override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        WindowName = $"TRANSFORM - {posing.Entity.FriendlyName}###brio_transform_window";

        PosingEditorCommon.DrawSelectionName(posing);

        DrawButtons(posing);
        ImGui.Separator();
        DrawGizmo();
        ImGui.Separator();

        _posingTransformEditor.Draw("overlay_transforms_edit", posing);

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

        // Check for multi-actor selection
        var selectedActors = new List<(ActorEntity actor, PosingCapability capability, Transform transform)>();
        
        foreach(var entityId in _entityManager.SelectedEntityIds)
        {
            if(_entityManager.TryGetEntity(entityId, out var entity) && 
               entity is ActorEntity actorEntity &&
               actorEntity.TryGetCapability<PosingCapability>(out var cap))
            {
                selectedActors.Add((actorEntity, cap, cap.ModelPosing.Transform));
            }
        }

        bool isMultiActorSelection = selectedActors.Count > 1;
        Vector3? multiActorCentroid = null;

        if(isMultiActorSelection)
        {
            multiActorCentroid = GroupedTransformHelper.CalculateCentroid(selectedActors.Select(a => a.transform));
        }

        var currentTransform = posing.ModelPosing.Transform;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        Matrix4x4? targetMatrix = selected.Match<Matrix4x4?>(
            (boneSelect) =>
            {
                if(isMultiActorSelection)
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
            _ => isMultiActorSelection ? 
                new Transform { Position = multiActorCentroid!.Value, Rotation = Quaternion.Identity, Scale = Vector3.One }.ToMatrix() :
                posing.ModelPosing.Transform.ToMatrix(),
            _ => isMultiActorSelection ? 
                new Transform { Position = multiActorCentroid!.Value, Rotation = Quaternion.Identity, Scale = Vector3.One }.ToMatrix() :
                posing.ModelPosing.Transform.ToMatrix()
        );

        if(targetMatrix == null)
            return;
            
        var matrix = _trackingMatrix ?? targetMatrix.Value;
        var originalMatrix = matrix;


        if(ImBrio.FontIconButton((_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom)))
            _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.World ? "Switch to Local" : "Switch to World");

        if(isMultiActorSelection)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1), $"({selectedActors.Count} actors)");
        }

        Vector2 gizmoSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X);

        if(ImBrioGizmo.DrawRotation(ref matrix, gizmoSize, _posingService.CoordinateMode == PosingCoordinateMode.World))
        {
            bool canEdit = !posing.ModelPosing.Freeze && !(selectedBone != null && selectedBone.Freeze);
            
            if(isMultiActorSelection)
            {
                canEdit = selectedActors.Any(a => !a.capability.ModelPosing.Freeze);
            }

            if(canEdit)
            {
                if(_groupedPendingSnapshot == null && _trackingMatrix == null)
                {
                    var list = new List<(EntityId, PoseInfo, Transform)>();
                    foreach(var id in _entityManager.SelectedEntityIds)
                    {
                        if(!_entityManager.TryGetEntity(id, out var ent))
                            continue;

                        if(!ent.TryGetCapability<PosingCapability>(out var cap))
                            continue;

                        list.Add((id, cap.SkeletonPosing.PoseInfo.Clone(), cap.ModelPosing.Transform));
                    }
                    _groupedPendingSnapshot = list;
                }

                _trackingMatrix = matrix;
            }
        }

        if(_trackingMatrix.HasValue)
        {
            var delta = _trackingMatrix.Value.ToTransform().CalculateDiff(originalMatrix.ToTransform());

            if(isMultiActorSelection)
            {
                foreach(var (actor, capability, originalTransform) in selectedActors)
                {
                    if(capability.ModelPosing.Freeze)
                        continue;

                    var newTransform = GroupedTransformHelper.ApplyRotationDeltaAroundPivot(
                        originalTransform,
                        multiActorCentroid!.Value,
                        delta.Rotation
                    );

                    capability.ModelPosing.Transform = newTransform;
                }
            }
            else
            {
                selected.Switch(
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
                    _ => posing.ModelPosing.Transform += delta,
                    _ => posing.ModelPosing.Transform += delta
                );
            }
        }

        if(!ImBrioGizmo.IsUsing() && _trackingMatrix.HasValue)
        {
            if(_groupedPendingSnapshot != null && _groupedPendingSnapshot.Count > 0)
            {
                _historyService.Snapshot(_groupedPendingSnapshot);
                _groupedPendingSnapshot = null;
            }

            foreach(var eid in _entityManager.SelectedEntityIds)
            {
                if(!_entityManager.TryGetEntity(eid, out var ent))
                    continue;

                if(!ent.TryGetCapability<PosingCapability>(out var cap))
                    continue;

                cap.Snapshot(false, false);
            }
            
            _trackingMatrix = null;
        }
    }
}
