using Brio.Capabilities.Actor;
using Brio.UI.Widgets.Core;
using Brio.UI.Controls.Editors;

namespace Brio.UI.Widgets.Actor;

internal class ActionTimelineWidget(ActionTimelineCapability capability) : Widget<ActionTimelineCapability>(capability)
{
    public override string HeaderName => "Action Timelines";

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
