using Brio.Capabilities.Actor;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;
public class ActorDynamicPoseWidget(ActorDynamicPoseCapability capability) : Widget<ActorDynamicPoseCapability>(capability)
{
    public override string HeaderName => "Dynamic Face Control";

    public override WidgetFlags Flags => Capability.Actor.IsProp ? WidgetFlags.CanHide :
        WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.HasAdvanced;

    bool eyes;
    bool body;
    bool head;

    int selected;

    public override void DrawBody()
    {



        ImGui.Separator();
        ImBrio.ToggleButtonStrip("DynamicFaceControlSelector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Eyes", "Body", "Head"]);

        switch(selected)
        {
            case 0:

                break;
            case 1:

                break;
            case 2:

                break;
        }

        if(ImBrio.ToggelButton("eyes", eyes))
        {
            eyes = !eyes;
        }
        ImGui.SameLine();
        if(ImBrio.ToggelButton("body", body))
        {
            body = !body;
        }
        ImGui.SameLine();
        if(ImBrio.ToggelButton("head", head))
        {
            head = !head;
        }

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
