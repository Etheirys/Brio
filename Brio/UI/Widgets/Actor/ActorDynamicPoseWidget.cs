using Brio.Capabilities.Actor;
using Brio.Game.Actor;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.UI.Widgets.Actor;

public class ActorDynamicPoseWidget(ActorDynamicPoseCapability capability) : Widget<ActorDynamicPoseCapability>(capability)
{
    public override string HeaderName => "Face Control";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    private static readonly ActionTimelineSelector _expressionSelector = new("expression_selector") { ExpressionsOnly = true };

    bool eyes = false;
    bool body = false;
    bool head = false;

    bool eyesLock = false;
    bool bodyLock = false;
    bool headLock = false;

    int selected = -1;
    Vector3 cameraVector3;
    public override void DrawBody()
    {
        _expressionSelector.DrawAsWindow();

        HandleExpressionSelectorChanges();

        if(Capability.Camera is not null)
            cameraVector3 = Capability.Camera.RealPosition;

        if(Capability.GameObject.ObjectKind != ObjectKind.Pc)
        {
            ImGui.TextWrapped("Please select a valid actor to use Dynamic Face Control.");
            return;
        }

        if(ImBrio.Button("   Set Expression", FontAwesomeIcon.Grin, new Vector2(ImBrio.GetRemainingWidth() - (28 * ImGuiHelpers.GlobalScale), 24 * ImGuiHelpers.GlobalScale), centerTest: true))
        {
            _expressionSelector.Select(null, false);
            ImGui.OpenPopup("dfc_expression_popup");
        }

        ImGui.SameLine();

        bool hasExpression = Capability.Actor.TryGetCapability<ActionTimelineCapability>(out var actionTimeline)
            && actionTimeline.HasSlotSpeedOverride(ActionTimelineSlots.Facial);

        if(ImBrio.FontIconButtonRight("reset_expression", FontAwesomeIcon.Undo, 1, "Reset Expression", hasExpression))
        {
            if(actionTimeline is not null)
            {
                actionTimeline.ResetSlotSpeedOverride(ActionTimelineSlots.Facial);
                actionTimeline.SlotedBlendAnimation = 604; // this is the emote for "Straight face"
                actionTimeline.BlendTimeline((ushort)actionTimeline.SlotedBlendAnimation);

                actionTimeline.ResetSlotSpeedOverride(ActionTimelineSlots.Facial);
                actionTimeline.SlotedBlendAnimation = 0;
                actionTimeline.BlendTimeline(3);
            }
        }

        using(var popup = ImRaii.Popup("dfc_expression_popup"))
        {
            if(popup.Success)
            {
                _expressionSelector.Draw();
            }
        }

        if(ImBrio.SeparatorTextButton("Dynamic Face Control", FontAwesomeIcon.PowerOff, tooltip: Capability.IsEnabled ? "Disable Face Control" : "Enable Face Control", toggled: Capability.IsEnabled))
        {
            Capability.IsEnabled = !Capability.IsEnabled;

            if(Capability.IsEnabled)
            {
                Capability.StartLookAt();
                Reset();
                Capability.SetTargetLock(false, LookAtTargetType.All, cameraVector3);
            }
            else
            {
                Capability.StopLookAt();
                selected = -1;
            }
        }

        using(ImRaii.Group())
        using(ImRaii.Disabled(!Capability.IsEnabled))
        {
            ImBrio.VerticalPadding(5);

            if(ImBrio.ButtonSelectorStrip("DynamicFaceControlSelector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Camera", "Position", "Actor"]))
            {
                Reset();

                Capability.SetTargetLock(false, LookAtTargetType.All, cameraVector3);

                switch(selected)
                {
                    case 0:
                        Capability.SetMode(LookAtTargetMode.Camera);
                        break;
                    case 1:
                        Capability.SetMode(LookAtTargetMode.Position);
                        break;
                    case 2:
                        Capability.SetMode(LookAtTargetMode.Target);
                        break;
                }
            }


            switch(selected)
            {
                case 0:
                    ImBrio.VerticalPadding(5);
                    DrawCamera();
                    ImBrio.VerticalPadding(5);
                    break;
                case 1:
                    ImBrio.VerticalPadding(5);
                    DrawPosition();
                    ImBrio.VerticalPadding(5);
                    break;
                case 2:
                    ImBrio.VerticalPadding(5);
                    DrawActor();
                    ImBrio.VerticalPadding(5);
                    break;
                default:
                    ImBrio.VerticalPadding(5);
                    break;
            }
        }
        if(!Capability.IsEnabled)
            ImBrio.AttachToolTip("Enable Face Control to use this feature.");
    }

    private void HandleExpressionSelectorChanges()
    {
        if(!Capability.Actor.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
            return;

        if(_expressionSelector.SoftSelectionChanged && _expressionSelector.SoftSelected != null)
        {
            actionTimeline.SlotedBlendAnimation = _expressionSelector.SoftSelected.TimelineId;

            if(actionTimeline.HasSlotSpeedOverride(ActionTimelineSlots.Facial))
            {
                actionTimeline.SlotedBlendAnimation = _expressionSelector.SoftSelected.TimelineId;
                actionTimeline.BlendTimeline((ushort)actionTimeline.SlotedBlendAnimation);
                actionTimeline.SetSlotSpeedOverride(ActionTimelineSlots.Facial, 0.0f);
            }
        }

        if(_expressionSelector.SelectionChanged && _expressionSelector.Selected != null)
        {
            actionTimeline.SlotedBlendAnimation = _expressionSelector.Selected.TimelineId;
            actionTimeline.BlendTimeline((ushort)actionTimeline.SlotedBlendAnimation);
            actionTimeline.SetSlotSpeedOverride(ActionTimelineSlots.Facial, 0.0f);
        }
    }

    public void Reset()
    {
        eyes = false;
        body = false;
        head = false;

        eyesLock = false;
        bodyLock = false;
        headLock = false;
    }

    public void DrawActor()
    {
        ImBrio.CenterNextElementWithPadding(75);
        if(ImGui.BeginCombo($"###actorsWidget_{Capability.Entity.Id}_list", Capability.SelectedActorName))
        {
            foreach(var value in Capability.EntityManager.TryGetAllActors())
            {
                if(value == Capability.Actor || value.GameObject.ObjectKind != ObjectKind.Pc)
                    continue;

                if(ImGui.Selectable($"[ {value.FriendlyName} ]"))
                {
                    Capability.SetTargetType(LookAtTargetType.All);

                    Capability.SetActorTarget(true, LookAtTargetType.All, value.GameObject.GameObjectId);

                    Capability.SelectedActorName = value.FriendlyName;
                    Capability.IsSelectingActor = true;
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset_selected", FontAwesomeIcon.Undo, 1f, "Reset Selected Actor", Capability.IsSelectingActor))
        {
            Capability.SetMode(LookAtTargetMode.None);

            Capability.SetTargetType(LookAtTargetType.None);

            Capability.SetActorTarget(false, LookAtTargetType.All, 0);

            Capability.IsSelectingActor = false;
            Capability.SelectedActorName = "Select an actor to track";
        }
    }

    public void DrawCamera()
    {
        (bool changed, bool active) df3h;
        using(ImRaii.Disabled(true))
            df3h = ImBrio.DragFloat3Implementation($"###dynamicFaceControlSelector_drag3", ref cameraVector3, 1);

        var size = ImBrio.GetRemainingWidth() / 3;
        (bool eyetoggle, bool eyelock) = ImBrio.ToggleLock("Eyes", size, ref eyes, ref eyesLock, disableOnLock: true);
        ImGui.SameLine();
        (bool bodytoggle, bool bodylock) = ImBrio.ToggleLock("Body", size, ref body, ref bodyLock, disableOnLock: true);
        ImGui.SameLine();
        (bool headtoggle, bool headlock) = ImBrio.ToggleLock("Head", size, ref head, ref headLock, disableOnLock: true);

        if(eyetoggle || bodytoggle || headtoggle)
        {
            LookAtTargetType lookAtTarget = default;
            if(body)
                lookAtTarget |= LookAtTargetType.Body;
            if(head)
                lookAtTarget |= LookAtTargetType.Head;
            if(eyes)
                lookAtTarget |= LookAtTargetType.Eyes;

            Capability.SetTargetType(lookAtTarget);
        }

        if(eyelock || bodylock || headlock)
        {
            if(eyelock)
                if(eyesLock)
                    Capability.SetTargetLock(true, LookAtTargetType.Eyes, cameraVector3);
                else
                    Capability.SetTargetLock(false, LookAtTargetType.Eyes, Capability.GetData()?.HeadTarget ?? cameraVector3);

            if(bodylock)
                if(bodyLock)
                    Capability.SetTargetLock(true, LookAtTargetType.Body, cameraVector3);
                else
                    Capability.SetTargetLock(false, LookAtTargetType.Body, Capability.GetData()?.BodyTarget ?? cameraVector3);

            if(headlock)
                if(headLock)
                    Capability.SetTargetLock(true, LookAtTargetType.Head, cameraVector3);
                else
                    Capability.SetTargetLock(false, LookAtTargetType.Head, Capability.GetData()?.HeadTarget ?? cameraVector3);
        }
    }

    Vector3 eyesVector3;
    Vector3 bodyVector3;
    Vector3 headVector3;
    public void DrawPosition()
    {
        (bool changed, bool active) eyesVectorDrag;
        (bool changed, bool active) bodyVectorDrag;
        (bool changed, bool active) headVectorDrag;

        bool eyetoggle = false;
        bool bodytoggle = false;
        bool headtoggle = false;

        if(ImBrio.ToggelButton($"Eyes###toggleButton_Eyes", new Vector2(53, 25), eyes))
        {
            eyetoggle = true;
            eyes = !eyes;
        }

        ImGui.SameLine();

        using(ImRaii.Disabled(!eyes))
        {
            if(ImBrio.FontIconButton("###dynamicFaceControlSelector_Eyes_button", FontAwesomeIcon.LocationCrosshairs, "Set to camera value"))
            {
                eyesVector3 = cameraVector3;
                Capability.SetTargetLock(true, LookAtTargetType.Eyes, eyesVector3);
                eyesLock = true;
            }
            ImGui.SameLine();
            eyesVectorDrag = ImBrio.DragFloat3Implementation($"###dynamicFaceControlSelector_Eyes_drag3", ref eyesVector3, 1);
        }

        if(ImBrio.ToggelButton($"Body###toggleButton_Body", new Vector2(53, 25), body))
        {
            bodytoggle = true;
            body = !body;
        }

        ImGui.SameLine();

        using(ImRaii.Disabled(!body))
        {
            if(ImBrio.FontIconButton("###dynamicFaceControlSelector_Body_button", FontAwesomeIcon.LocationCrosshairs, "Set to camera value"))
            {
                bodyVector3 = cameraVector3;
                Capability.SetTargetLock(true, LookAtTargetType.Body, bodyVector3);
                bodyLock = true;
            }
            ImGui.SameLine();
            bodyVectorDrag = ImBrio.DragFloat3Implementation($"###dynamicFaceControlSelector_Body_drag3", ref bodyVector3, 1);
        }

        if(ImBrio.ToggelButton($"Head###toggleButton_Head", new Vector2(53, 25), head))
        {
            headtoggle = true;
            head = !head;
        }
        ImGui.SameLine();

        using(ImRaii.Disabled(!head))
        {
            if(ImBrio.FontIconButton("###dynamicFaceControlSelector_Head_button", FontAwesomeIcon.LocationCrosshairs, "Set to camera value"))
            {
                headVector3 = cameraVector3;
                Capability.SetTargetLock(true, LookAtTargetType.Head, headVector3);
                headLock = true;
            }
            ImGui.SameLine();
            headVectorDrag = ImBrio.DragFloat3Implementation($"###dynamicFaceControlSelector_Head_drag3", ref headVector3, 1);
        }

        if(headVectorDrag.changed || headVectorDrag.active)
        {
            Capability.SetTargetLock(true, LookAtTargetType.Head, headVector3);
            headLock = true;
        }

        if(bodyVectorDrag.changed || bodyVectorDrag.active)
        {
            Capability.SetTargetLock(true, LookAtTargetType.Body, bodyVector3);
            bodyLock = true;
        }

        if(eyesVectorDrag.changed || eyesVectorDrag.active)
        {
            Capability.SetTargetLock(true, LookAtTargetType.Eyes, eyesVector3);
            eyesLock = true;
        }

        if(eyetoggle || bodytoggle || headtoggle)
        {
            LookAtTargetType lookAtTarget = default;
            if(body)
                lookAtTarget |= LookAtTargetType.Body;
            if(head)
                lookAtTarget |= LookAtTargetType.Head;
            if(eyes)
                lookAtTarget |= LookAtTargetType.Eyes;

            Capability.SetTargetType(lookAtTarget);
        }

        var data = Capability.GetData();
        if(data is not null)
        {
            headVector3 = data.HeadTarget;
            bodyVector3 = data.BodyTarget;
            eyesVector3 = data.EyesTarget;
        }
    }

    public override void ToggleAdvancedWindow()
    {

    }
}
