namespace Brio.Config;

internal class InterfaceConfiguration
{
    public OpenBrioBehavior OpenBrioBehavior { get; set; } = OpenBrioBehavior.OnGPoseEnter;
    public bool ShowInGPose { get; set; } = true;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowWhenUIHidden { get; set; } = false;
    public bool CensorActorNames { get; set; } = false;
}
