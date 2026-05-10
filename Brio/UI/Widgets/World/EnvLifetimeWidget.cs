using Brio.Capabilities.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.World;

public class EnvLifetimeWidget(EnvironmentLifetimeCapability environmentLifetimeCapability) : Widget<EnvironmentLifetimeCapability>(environmentLifetimeCapability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(environmentLifetimeCapability.GPoseService.IsGPosing is false))
            if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "Spawn New"))
            {
                SpawnMenu.OpenUnifiedSpawnMenu();
            }
    }
}
