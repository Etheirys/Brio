using Brio.Capabilities.Actor;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.Actor;

internal class ActionTimelineWidget(ActionTimelineCapability capability) : Widget<ActionTimelineCapability>(capability)
{
    public override string HeaderName => "Animation Control";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.HasAdvanced;

    private readonly ActionTimelineEditor _editor = new ActionTimelineEditor();

    public override void DrawBody()
    {
        _editor.Draw(false, Capability);
    }

    public override void ActivateAdvanced()
    {
        UIManager.Instance.ShowActionTimelineWindow();
    }
}
