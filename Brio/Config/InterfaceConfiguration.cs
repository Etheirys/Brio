namespace Brio.Config;

public class InterfaceConfiguration
{
    public OpenBrioBehavior OpenBrioBehavior { get; set; } = OpenBrioBehavior.OnGPoseEnter;
    public bool ShowInGPose { get; set; } = true;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowWhenUIHidden { get; set; } = false;
    public bool CensorActorNames { get; set; } = true;

    // Transform Movement Speed
    public float DefaultTransformMovementSpeed { get; set; } = 0.01f;

    // Bone Transform Movement Speed
    public float DefaultBoneTransformMovementSpeed { get; set; } = 0.01f;

    // Free Camera Movement Speed
    public float DefaultFreeCameraMovementSpeed { get; set; } = 0.03f;

    // Free Camera Mouse Sensitivity
    public float DefaultFreeCameraMouseSensitivity { get; set; } = 0.1f;
}
