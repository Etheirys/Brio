using Brio.Input;
using Dalamud.Game.ClientState.Keys;

namespace Brio.Config;

internal class InterfaceConfiguration
{
    public OpenBrioBehavior OpenBrioBehavior { get; set; } = OpenBrioBehavior.OnGPoseEnter;
    public bool ShowInGPose { get; set; } = true;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowWhenUIHidden { get; set; } = false;
    public bool CensorActorNames { get; set; } = false;

    public KeyBind ToggleBrioWindowKeyBind { get; set; } = new(VirtualKey.B, true);
    public KeyBind IncrementSmallModifierKeyBind { get; set; } = new(VirtualKey.NO_KEY);
    public KeyBind IncrementLargeModifierKeyBind { get; set; } = new(VirtualKey.NO_KEY);
}
