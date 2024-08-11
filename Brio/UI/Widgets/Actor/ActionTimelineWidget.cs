using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Game.Posing;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.Actor;

internal class ActionTimelineWidget(ActionTimelineCapability capability, PhysicsService physicsService, ConfigurationService configService) : Widget<ActionTimelineCapability>(capability)
{
    public override string HeaderName => "Animation Control";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.HasAdvanced;

    private readonly ActionTimelineEditor _editor = new(null!, null!, physicsService, configService);

    public override void DrawBody()
    {
        _editor.Draw(false, Capability);
    }

    public override void ActivateAdvanced()
    {
        UIManager.Instance.ShowActionTimelineWindow();
    }
}
