using Brio.Capabilities.WorldObjects;
using Brio.Game.WorldObjects.Objects;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.WorldObjects;

public class DebugWorldObjectWidget(DebugWorldObjectCapability objectCapability) : Widget<DebugWorldObjectCapability>(objectCapability)
{
    public override string HeaderName => "Debug";
    public override WidgetFlags Flags => Capability.IsDebug ? WidgetFlags.DrawBody : WidgetFlags.None;

    public unsafe override void DrawBody()
    {
        Capability._dynamisIPC.DrawPointer(Capability.GameBgObject.Address);

        if(Capability.GameBgObject is StaticVfxObject vfxObject)
        {
            foreach(var child in vfxObject.VFX->ChildObjects)
            {
                Capability._dynamisIPC.DrawPointer(&child);
            }
        }
    }
}
