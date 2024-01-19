using Brio.Capabilities.Camera;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal static class CameraEditor
{
    public unsafe static void Draw(string id, CameraCapability capability)
    {
        var camera = capability.Camera;

        using(ImRaii.PushId(id))
        {
            using(ImRaii.Disabled(!capability.IsAllowed))
            {
                if(camera != null)
                {
                    var width = -ImGui.CalcTextSize("XXXXXXXXXx").X;

                    const string offsetText = "Offset";
                    Vector3 pos = capability.PositionOffset;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat3(offsetText, ref pos, 0.001f))
                        capability.PositionOffset = pos;

                    ImGui.SameLine();

                    if(ImBrio.FontIconButtonRight("reset", Dalamud.Interface.FontAwesomeIcon.Undo, 1.2f, "Reset", capability.IsOveridden))
                        capability.Reset();

                    const string rotationText = "Rotation";
                    float rotation = camera->Rotation;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.SliderAngle(rotationText, ref rotation, -180, 180, "%.2f"))
                        camera->Rotation = rotation;

                    const string zoomText = "Zoom";
                    float zoom = camera->Camera.Distance;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.SliderFloat(zoomText, ref zoom, camera->Camera.MaxDistance, camera->Camera.MinDistance, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera->Camera.Distance = zoom;

                    const string fovText = "FoV";
                    float fov = camera->FoV;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.SliderAngle(fovText, ref fov, -44, 120, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera->FoV = fov;

                    const string panText = "Pan";
                    Vector2 pan = camera->Pan;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(panText, ref pan, 0.001f))
                        camera->Pan = pan;

                    const string angleText = "Angle";
                    Vector2 angle = camera->Angle;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.DragFloat2(angleText, ref angle, 0.001f))
                        camera->Angle = angle;

                    var disable = capability.DisableCollision;
                    if(ImGui.Checkbox("Disable Collision", ref disable))
                        capability.DisableCollision = disable;

                    ImGui.SameLine();

                    var delimit = capability.DelimitCamera;
                    if(ImGui.Checkbox("Delimit Camera", ref delimit))
                        capability.DelimitCamera = delimit;
                }
            }
        }
    }
}
