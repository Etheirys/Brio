using Brio.Capabilities.Camera;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal static class CameraEditor
{
    struct CameraPresetProperties
    {
        public bool isSet = false;
        public Vector3 offset;
        public float rotation;
        public float zoom;
        public float fov;
        public Vector2 pan;
        public Vector2 angle;
        public bool disableCollision;
        public bool delimitCamera;

        public CameraPresetProperties(Vector3 offset, float rotation, float zoom, float fov, Vector2 pan, Vector2 angle, bool disableCollision, bool delimitCamera)
        {
            this.offset = offset;
            this.rotation = rotation;
            this.zoom = zoom;
            this.fov = fov;
            this.pan = pan;
            this.angle = angle;
            this.disableCollision = disableCollision;
            this.delimitCamera = delimitCamera;

            this.isSet = true;
        }
    }
    private static CameraPresetProperties[] presets = new CameraPresetProperties[3];

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
                    if(ImBrio.SliderAngle(rotationText, ref rotation, -180, 180, "%.2f"))
                        camera->Rotation = rotation;

                    const string zoomText = "Zoom";
                    float zoom = camera->Camera.Distance;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderFloat(zoomText, ref zoom, camera->Camera.MaxDistance, camera->Camera.MinDistance, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                        camera->Camera.Distance = zoom;

                    const string fovText = "FoV";
                    float fov = camera->FoV;
                    ImGui.SetNextItemWidth(width);
                    if(ImBrio.SliderAngle(fovText, ref fov, -44, 120, "%.2f", ImGuiSliderFlags.AlwaysClamp))
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

                    ImGui.Separator();
                    if(ImGui.CollapsingHeader("Camera Presets"))
                    {
                        for(int i = 0; i < 3; i++)
                        {
                            ImGui.Text("Preset " + (i + 1).ToString() + ": ");
                            ImGui.SameLine();
                            if(ImGui.Button("Save##" + i.ToString()))
                                presets[i] = new CameraPresetProperties(capability.PositionOffset, camera->Rotation, camera->Camera.Distance, camera->FoV, camera->Pan, camera->Angle, capability.DisableCollision, capability.DelimitCamera);
                            if(presets[i].isSet)
                            {
                                ImGui.SameLine();
                                if(ImGui.Button("Load##" + i.ToString()))
                                {
                                    capability.PositionOffset = presets[i].offset;
                                    camera->Rotation = presets[i].rotation;
                                    camera->Camera.Distance = presets[i].zoom;
                                    camera->FoV = presets[i].fov;
                                    camera->Pan = presets[i].pan;
                                    camera->Angle = presets[i].angle;
                                    capability.DisableCollision = presets[i].disableCollision;
                                    capability.DelimitCamera = presets[i].delimitCamera;
                                }
                            }
                        }
                    }

                }
            }
        }
    }
}
