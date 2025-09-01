using Brio.Capabilities.Actor;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
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

    bool eyesLock;
    bool bodyLock;
    bool headLock;

    int selected;

    bool enable;
    public override void DrawBody()
    {
        using(ImRaii.Disabled(true))
        {

            if(ImBrio.ToggelButton("Enable Face Control", enable))
            {
                enable = !enable;

                if(enable)
                    Capability.TESTactorlook();
                else
                    Capability.TESTactorlookClear();
            }

            ImGui.Separator();

            ImBrio.ToggleButtonStrip("DynamicFaceControlSelector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Camera", "Position", "Actor"]);

            switch(selected)
            {
                case 0:

                    break;
                case 1:

                    break;
                case 2:

                    break;
            }

            var size = ImBrio.GetRemainingWidth() / 3;

            ImBrio.ToggleLock("Eyes", size, ref eyes, ref eyesLock, disableOnLock: true);
            ImGui.SameLine();
            ImBrio.ToggleLock("Body", size, ref body, ref bodyLock, disableOnLock: true);
            ImGui.SameLine();
            ImBrio.ToggleLock("Head", size, ref head, ref headLock, disableOnLock: true);
        }

    }

    public override void ToggleAdvancedWindow()
    {

    }
}
