using Brio.Capabilities.Camera;
using Brio.Config;
using Brio.Entities.Camera;
using Brio.Files;
using Brio.Game.Camera;
using Brio.Game.Cutscene;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.IO;
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
                if(ImGui.Button("New Brio Camera"u8, new(155 * ImGuiHelpers.GlobalScale, 0)))
                {
                    virtualCameraManager.CreateCamera(CameraType.Game);
                }

                if(ImGui.Button("New Free-Cam"u8, new(155 * ImGuiHelpers.GlobalScale, 0)))
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

                if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1f, "Reset Camera", camera.IsOverridden))
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
                    if(ImBrio.DragFloat2V3("Pan", ref rotation, -360, 360, "%.3f", true, ImGuiSliderFlags.AlwaysClamp))
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

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetPivotRotation", FontAwesomeIcon.Undo, 1f, "Reset Pivot", camera.PivotRotation != 0))
                        camera.PivotRotation = 0;
                }

                ImGui.Separator();

                {
                    ImBrio.Icon(FontAwesomeIcon.Walking);

                    ImGui.SameLine();

                    float moveSpeed = camera.FreeCamValues.MovementSpeed;
                    if(ImBrio.SliderFloat("##MovementSpeed", ref moveSpeed, 0.005f, 0.3f, "%.4f", ImGuiSliderFlags.None, step: 0.001f))
                        camera.FreeCamValues.MovementSpeed = moveSpeed;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetMovementSpeed", FontAwesomeIcon.Undo, 1f, "Reset Movement Speed", moveSpeed != capability.configurationService.Configuration.Interface.DefaultFreeCameraMovementSpeed))
                        camera.FreeCamValues.MovementSpeed = capability.configurationService.Configuration.Interface.DefaultFreeCameraMovementSpeed;
                }

                {
                    ImBrio.Icon(FontAwesomeIcon.Mouse);

                    ImGui.SameLine();

                    float mouseSpeed = camera.FreeCamValues.MouseSensitivity;
                    if(ImBrio.SliderFloat("##MouseSensitivity", ref mouseSpeed, 0.001f, 0.2f, "%.4f", ImGuiSliderFlags.None, step: 0.001f))
                        camera.FreeCamValues.MouseSensitivity = mouseSpeed;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("resetMouseSensitivity", FontAwesomeIcon.Undo, 1f, "Reset MouseSensitivity", mouseSpeed != capability.configurationService.Configuration.Interface.DefaultFreeCameraMouseSensitivity))
                        camera.FreeCamValues.MouseSensitivity = capability.configurationService.Configuration.Interface.DefaultFreeCameraMouseSensitivity;
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

    //
    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + (ImGui.GetStyle().FramePadding.X * 2f);

    public unsafe static void DrawBrioCutscene(string id, BrioCameraCapability _cameraCapability, CutsceneManager _cutsceneManager, ConfigurationService _configService)
    {
        var cameraValues = _cameraCapability.CameraEntity.VirtualCamera.CutsceneCamValues;

        ImGui.Text("Camera Path ");

        ImGui.InputText(string.Empty, ref cameraValues.CameraPath, 0, ImGuiInputTextFlags.ReadOnly);

        ImGui.SameLine();

        if(ImGui.Button("Browse"))
        {
            UIManager.Instance.FileDialogManager.OpenFileDialog("Browse for XAT Camera File", "XAT Camera File {.xcp}",
                (success, path) =>
                {
                    if(success)
                    {
                        cameraValues.CameraPath = path[0];

                        string? folderPath = Path.GetDirectoryName(cameraValues.CameraPath);
                        if(folderPath is not null)
                        {
                            _configService.Configuration.LastXATPath = folderPath;
                            _configService.Save();

                            _cutsceneManager.CameraPath = new XATCameraFile(new BinaryReader(File.OpenRead(cameraValues.CameraPath)));
                        }
                    }
                    else
                    {
                        cameraValues.CameraPath = string.Empty;
                        _cutsceneManager.CameraPath = null;
                    }
                }, 1, _configService.Configuration.LastXATPath, false);
        }

        ImGui.Separator();

        using(ImRaii.Disabled(string.IsNullOrEmpty(cameraValues.CameraPath)))
        {
            ImGui.Checkbox("Enable FOV", ref _cutsceneManager.CameraSettings.EnableFOV);

            ImGui.Separator();

            ImGui.Text("Disabling FOV will make for a less accurate Camera, but might");
            ImGui.Text("provide for an easer way to support more character sizes without");
            ImGui.Text("changing the Camera's Scale & Offset!");

            ImGui.Separator();

            ImGui.InputFloat3("Camera Scale", ref _cutsceneManager.CameraSettings.Scale);
            ImGui.InputFloat3("Camera Offset", ref _cutsceneManager.CameraSettings.Offset);

            ImGui.Separator();

            ImGui.Checkbox("Loop", ref _cutsceneManager.CameraSettings.Loop);

            ImGui.Checkbox("Hide Brio On Play  (Press 'Shift + B' to Stop Cutscene)", ref _cutsceneManager.CloseWindowsOnPlay);

            ImGui.Checkbox("###delay_Start", ref _cutsceneManager.DelayStart);
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Start Delay");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxItemWidth);

            using(ImRaii.Disabled(_cutsceneManager.DelayStart == false))
            {
                ImGui.InputInt($"###delay_Start_Chek", ref _cutsceneManager.DelayTime, 0, 0);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(LabelStart);
            ImGui.Text("Start Delay");

            ImGui.Separator();

            ImGui.Checkbox("Start All Actors Animations On Play", ref _cutsceneManager.StartAllActorAnimationsOnPlay);

            using(ImRaii.Disabled(_cutsceneManager.StartAllActorAnimationsOnPlay == false))
            {
                ImGui.Checkbox("###animation_delay_Start", ref _cutsceneManager.DelayAnimationStart);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Animation Start Delay");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(MaxItemWidth);

                using(ImRaii.Disabled(_cutsceneManager.DelayAnimationStart == false))
                {
                    ImGui.InputInt($"###animation_delay_Start_Chek", ref _cutsceneManager.DelayAnimationTime, 0, 0);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(LabelStart);
                ImGui.Text("Animation Delay");
            }

            ImGui.Separator();

            ImGui.Text("The time-scale for the delay functions are in Milliseconds!");
            ImGui.Text("1000 Milliseconds = 1 Second");

            ImGui.Separator();

            var isrunning = _cutsceneManager.IsRunning;
            using(ImRaii.Disabled(isrunning))
            {
                if(ImGui.Button("Play"))
                {
                    _cutsceneManager.StartPlayback();
                }
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(!isrunning))
            {
                if(ImGui.Button("Stop"))
                {
                    _cutsceneManager.StopPlayback();
                }
            }
        }
    }
}
