using Brio.Capabilities.Actor;
using Brio.Game.Actor;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;
public class ActorDynamicPoseWidget(ActorDynamicPoseCapability capability) : Widget<ActorDynamicPoseCapability>(capability)
{
    public override string HeaderName => "Dynamic Face Control";

    public override WidgetFlags Flags => Capability.Actor.IsProp ? WidgetFlags.CanHide :
        WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    bool eyes;
    bool body;
    bool head;

    bool eyesLock;
    bool bodyLock;
    bool headLock;

    int selected = -1;

    bool enable;

    Vector3 cameraVector3;
    public override void DrawBody()
    {
        if(Capability.Camera is not null)
            cameraVector3 = Capability.Camera.RealPosition;

        if(ImBrio.ToggelButton("Enable Face Control", enable))
        {
            enable = !enable;

            if(enable)
                Capability.StartLookAt();
            else
                Capability.StopLookAt();
        }

        using(ImRaii.Disabled(!enable))
        {
            ImGui.Separator();

            if(ImBrio.ToggleButtonStrip("DynamicFaceControlSelector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Camera", "Position", "Actor"]))
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
                        Capability.SetMode(LookAtTargetMode.None);
                        break;
                }
            }

            switch(selected)
            {
                case 0:
                    DrawCamera();
                    break;
                case 1:
                    DrawPosition();
                    break;
                case 2:
                    using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                    {
                        ImGui.Text("Feature currently unavailable.");
                        ImGui.Text("Check back in a future update!");
                    }
                    break;
            }

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

    public void DrawCamera()
    {
        (bool changed, bool active) df3h;
        using(ImRaii.Disabled(true))
            df3h = ImBrio.DragFloat3Simple($"###dynamicFaceControlSelector_drag3", ref cameraVector3, 1);

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
                    Capability.SetTargetLock(false, LookAtTargetType.Eyes, Capability.GetData()!.HeadTarget);

            if(bodylock)
                if(bodyLock)
                    Capability.SetTargetLock(true, LookAtTargetType.Body, cameraVector3);
                else
                    Capability.SetTargetLock(false, LookAtTargetType.Body, Capability.GetData()!.BodyTarget);

            if(headlock)
                if(headLock)
                    Capability.SetTargetLock(true, LookAtTargetType.Head, cameraVector3);
                else
                    Capability.SetTargetLock(false, LookAtTargetType.Head, Capability.GetData()!.HeadTarget);
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

        if(ImBrio.ToggelButton($"Eyes###toggleButton_Eyes", new Vector2(53 * ImGuiHelpers.GlobalScale, 25 * ImGuiHelpers.GlobalScale), eyes))
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
            eyesVectorDrag = ImBrio.DragFloat3Simple($"###dynamicFaceControlSelector_Eyes_drag3", ref eyesVector3, 1);
        }

        if(ImBrio.ToggelButton($"Body###toggleButton_Body", new Vector2(53 * ImGuiHelpers.GlobalScale, 25 * ImGuiHelpers.GlobalScale), body))
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
            bodyVectorDrag = ImBrio.DragFloat3Simple($"###dynamicFaceControlSelector_Body_drag3", ref bodyVector3, 1);
        }

        if(ImBrio.ToggelButton($"Head###toggleButton_Head", new Vector2(53 * ImGuiHelpers.GlobalScale, 25 * ImGuiHelpers.GlobalScale), head))
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
            headVectorDrag = ImBrio.DragFloat3Simple($"###dynamicFaceControlSelector_Head_drag3", ref headVector3, 1);
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
