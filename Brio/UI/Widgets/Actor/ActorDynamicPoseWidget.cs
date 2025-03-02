using Brio.Capabilities.Actor;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using ImGuiNET;

namespace Brio.UI.Widgets.Actor;
public class ActorDynamicPoseWidget(ActorDynamicPoseCapability capability) : Widget<ActorDynamicPoseCapability>(capability)
{
    public override string HeaderName => "Dynamic Face Control";

    public override WidgetFlags Flags => Capability.Actor.IsProp ? WidgetFlags.CanHide :
        WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.HasAdvanced;

    public override void DrawBody()
    {
        if(ImBrio.FontIconButton("agc_appearance", FontAwesomeIcon.ArrowsToEye, "A Advanced"))
            Capability.TESTactorlook();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("agcc_appearance", FontAwesomeIcon.ArrowsToEye, "C Advanced"))
            Capability.TESTactorlookClear();

    }

    public override void ToggleAdvancedWindow()
    {

    }
}
