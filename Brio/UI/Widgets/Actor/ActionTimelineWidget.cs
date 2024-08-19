using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Game.Posing;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.Actor;

internal class ActionTimelineWidget(ActionTimelineCapability capability, EntityManager entityManager, PhysicsService physicsService, ConfigurationService configService) : Widget<ActionTimelineCapability>(capability)
{
    public override string HeaderName => "Animation Control";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.HasAdvanced;

    private readonly ActionTimelineEditor _editor = new(null!, null!, entityManager, physicsService, configService);

    public override void DrawBody()
    {
        _editor.Draw(false, Capability);
    }

    public override void ToggleAdvancedWindow()
    {
        UIManager.Instance.ToggleActionTimelineWindow();
    }
}
