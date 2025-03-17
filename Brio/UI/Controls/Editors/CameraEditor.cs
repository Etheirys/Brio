using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.Game.Camera;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class CameraEditor
{
    public unsafe static void DrawSpawnMenu(VirtualCameraManager virtualCameraManager)
    {
        using var popup = ImRaii.Popup("DrawSpawnMenuPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("New Brio Camera"))
                {
                    virtualCameraManager.CreateCamera(CameraType.Brio);
                }

                if(ImGui.Button("New Free-Cam"))
                {
                    virtualCameraManager.CreateCamera(CameraType.Free);
                }

                //ImGui.Separator();

                //if(ImGui.Button("New Cutscene Camera"))
                //{
                //    virtualCameraManager.CreateCamera(CameraType.Cutscene);
                //}
            }
        }
    }

    public unsafe static void DrawFreeCam(string id, BrioCameraCapability capability)
    {
        var camera = capability.VirtualCamera;

        using(ImRaii.PushId(id))
        {
            using(ImRaii.Disabled(!capability.IsAllowed))
            {
                var width = -ImGui.CalcTextSize("XXXXXXXx").X;

                if(ImBrio.ToggelButton("Enable Movement", new Vector2(135, 25), camera.FreeCamValues.IsMovementEnabled, hoverText: camera.FreeCamValues.IsMovementEnabled ?
                    "Disable Free-Cam Movement" : "Enabled Free-Cam Movement"))
                {
                    camera.FreeCamValues.IsMovementEnabled = !camera.FreeCamValues.IsMovementEnabled;
                }

                ImGui.SameLine();

                using(ImRaii.Disabled(camera.FreeCamValues.IsMovementEnabled == false))
                {
                    if(ImBrio.ToggelFontIconButton("LateralMovement", FontAwesomeIcon.SolarPanel, new Vector2(25, 0), camera.FreeCamValues.Move2D, hoverText: "Lateral Movement"))
                    {
                        camera.FreeCamValues.Move2D = !camera.FreeCamValues.Move2D;
                    }
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1f, "Reset", camera.IsOverridden))
                    camera.ResetCamera();

                //
                ImGui.Separator();
                //

                {
                    ImBrio.Icon(FontAwesomeIcon.ArrowsToCircle);

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(width);
                    var position = camera.Position;
                    if(ImGui.DragFloat3("Position", ref position, 0.001f))
                        camera.Position = position;
                }

                {
                    ImBrio.Icon(FontAwesomeIcon.ArrowsSpin);

                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(width);
                    var rotation = camera.Rotation;
                    if(ImBrio.DragFloat2V3("Rotation", ref rotation, -360, 360, "%.0f", true, ImGuiSliderFlags.AlwaysClamp))
                        camera.Rotation = rotation;
                }

                {
                    ImBrio.Icon(FontAwesomeIcon.Panorama);

                    ImGui.SameLine();

                    var fov = camera.FoV;
                    if(ImBrio.SliderAngle("##FoV", ref fov, -44, 120, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera.FoV = fov;
                }

                {
                    ImBrio.Icon(FontAwesomeIcon.CameraRotate);

                    ImGui.SameLine();

                    float pivot = camera.PivotRotation;
                    if(ImBrio.SliderAngle("##PivotRotation", ref pivot, -180, 180, "%.2f"))
                        camera.PivotRotation = pivot;
                }

                ImGui.Separator();

                {
                    ImBrio.Icon(FontAwesomeIcon.Walking);

                    ImGui.SameLine();

                    float moveSpeed = camera.FreeCamValues.MovementSpeed;
                    if(ImBrio.SliderFloat("##MovementSpeed", ref moveSpeed, 0.005f, 0.3f, "%.4f", ImGuiSliderFlags.None, step: 0.001f))
                        camera.FreeCamValues.MovementSpeed = moveSpeed;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetMovementSpeed", FontAwesomeIcon.Undo, 1f, "Reset Movement Speed", moveSpeed != VirtualCameraManager.DefaultMovementSpeed))
                        camera.FreeCamValues.MovementSpeed = VirtualCameraManager.DefaultMovementSpeed;
                }

                {
                    ImBrio.Icon(FontAwesomeIcon.Mouse);

                    ImGui.SameLine();

                    float mouseSpeed = camera.FreeCamValues.MouseSensitivity;
                    if(ImBrio.SliderFloat("##MouseSensitivity", ref mouseSpeed, 0.001f, 0.2f, "%.4f", ImGuiSliderFlags.None, step: 0.001f))
                        camera.FreeCamValues.MouseSensitivity = mouseSpeed;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetMouseSensitivity", FontAwesomeIcon.Undo, 1f, "Reset MouseSensitivity", mouseSpeed != VirtualCameraManager.DefaultMouseSensitivity))
                        camera.FreeCamValues.MouseSensitivity = VirtualCameraManager.DefaultMouseSensitivity;
                }

                ImGui.Separator();

                var delimit = camera.FreeCamValues.DelimitAngle;
                if(ImGui.Checkbox("Delimit Camera Angle", ref delimit))
                    camera.FreeCamValues.DelimitAngle = delimit;
            }
        }
    }

    public unsafe static void DrawBrioCam(string id, BrioCameraCapability capability)
    {
        var camera = capability.VirtualCamera;

        using(ImRaii.PushId(id))
        {
            using(ImRaii.Disabled(!capability.IsAllowed))
            {
                if(camera is not null)
                {
                    var width = -ImGui.CalcTextSize("XXXXXXXXXx").X;

                    if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1f, "Reset", camera.IsOverridden))
                        camera.ResetCamera();

                    //
                    ImGui.Separator();
                    //

                    {
                        const string offsetText = "Position";

                        ImBrio.Icon(FontAwesomeIcon.ArrowsToCircle);

                        ImGui.SameLine();

                        ImGui.SetNextItemWidth(width);
                        var position = camera.RealPosition;
                        using(ImRaii.Disabled(true))
                        {
                            ImGui.DragFloat3(offsetText, ref position, 0.001f);
                        }
                    }

                    {
                        const string offsetText = "Offset";

                        ImBrio.Icon(FontAwesomeIcon.ArrowsUpDownLeftRight);

                        ImGui.SameLine();

                        Vector3 pos = camera.PositionOffset;
                        ImGui.SetNextItemWidth(width);
                        if(ImGui.DragFloat3(offsetText, ref pos, 0.001f))
                            camera.PositionOffset = pos;

                        ImGui.SameLine();

                        if(ImBrio.FontIconButtonRight("resetPosition", FontAwesomeIcon.Undo, 1f, "Reset Position-Offset", camera.PositionOffset != Vector3.Zero))
                            camera.PositionOffset = Vector3.Zero;
                    }

                    ImGui.Separator();

                    const string rotationText = "Rotation";
                    float rotation = camera.PivotRotation;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderAngle(rotationText, ref rotation, -180, 180, "%.2f"))
                        camera.PivotRotation = rotation;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetRotation", FontAwesomeIcon.Undo, 1f, "Reset", rotation != 0))
                        camera.PivotRotation = 0;

                    const string zoomText = "Zoom";
                    float zoom = camera.Zoom;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderFloat(zoomText, ref zoom, camera.BrioCamera->Camera.MaxDistance, camera.BrioCamera->Camera.MinDistance, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera.Zoom = zoom;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetZoom", FontAwesomeIcon.Undo, 1f, "Reset", zoom != 2.5))
                        camera.Zoom = 2.5f;

                    const string fovText = "FoV";
                    float fov = camera.FoV;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderAngle(fovText, ref fov, -44, 120, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera.FoV = fov;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetFoV", FontAwesomeIcon.Undo, 1f, "Reset", fov != 0))
                        camera.FoV = 0f;

                    ImGui.Separator();

                    const string panText = "Pan";
                    Vector2 pan = camera.Pan;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(panText, ref pan, 0.001f))
                        camera.Pan = pan;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetPan", FontAwesomeIcon.Undo, 1f, "Reset", pan != Vector2.Zero))
                        camera.Pan = Vector2.Zero;

                    const string angleText = "Angle";
                    Vector2 angle = camera.Angle;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(angleText, ref angle, 0.001f))
                        camera.Angle = angle;

                    ImGui.Separator();

                    var disable = camera.DisableCollision;
                    if(ImGui.Checkbox("Disable Collision", ref disable))
                        camera.DisableCollision = disable;

                    ImGui.SameLine();

                    var delimit = camera.DelimitCamera;
                    if(ImGui.Checkbox("Delimit Camera", ref delimit))
                        camera.DelimitCamera = delimit;
                }
            }
        }
    }
}
