using Dalamud.Configuration;
using System;

namespace Brio.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 1;
    public const int CurrentPopupKey = 1;
    public int Version { get; set; } = CurrentVersion;

    // First Time User
    public bool IsFirstTimeUser { get; set; } = true;
    public int PopupKey { get; set; } = -1;

    // Interface
    public OpenBrioBehavior OpenBrioBehavior { get; set; } = OpenBrioBehavior.OnGPoseEnter;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowWhenUIHidden { get; set; } = false;

    // Hooks
    public ApplyNPCHack ApplyNPCHack { get; set; } = ApplyNPCHack.InGPose;

    // Integrations
    public bool AllowPenumbraIntegration { get; set; } = true;
    public bool AllowWebAPI { get; set; } = false;

    // Developer
    public bool ForceDebug { get; set; } = false;
}

public enum OpenBrioBehavior
{
    Manual,
    OnGPoseEnter,
    OnPluginStartup
}

public enum ApplyNPCHack
{
    Manual,
    InGPose,
    Always
}
