using Brio.Capabilities.Camera;
using Brio.Entities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Camera;

public class BrioCameraWidget(BrioCameraCapability capability) : Widget<BrioCameraCapability>(capability)
{
    public override string HeaderName => "Camera Editor";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.HasAdvanced;

    public unsafe override void DrawBody()
    {
        if(Capability.CameraEntity.CameraType == CameraType.Free)
        {
            CameraEditor.DrawFreeCam("camera_widget_editor", Capability);
        }
        else if(Capability.CameraEntity.CameraType == CameraType.Cutscene)
        {
            if(ImGui.Button("Open Camera Window"))
            {
                Capability.ShowCameraWindow();
            }
            ImBrio.TextCentered("Open the Camera Window to edit play a Cutscene ", ImGui.GetWindowContentRegionMax().X);

        }
        else
        {
            CameraEditor.DrawBrioCam("camera_widget_editor", Capability);
        }
    }

    public override void ToggleAdvancedWindow() => Capability.ShowCameraWindow();
}
