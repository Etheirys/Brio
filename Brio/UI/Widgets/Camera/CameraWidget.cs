using Brio.Capabilities.Camera;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.Camera;

internal class CameraWidget : Widget<CameraCapability>
{
    public override string HeaderName => "Camera";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.HasAdvanced;

    public CameraWidget(CameraCapability capability) : base(capability)
    {
    }

    public unsafe override void DrawBody()
    {
        CameraEditor.Draw("camera_widget_editor", Capability);
    }

    public override void ActivateAdvanced() => Capability.ShowCameraWindow();
}
