using Dalamud.Configuration;
using System;

namespace Brio.Config;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 1;
    public int Version { get; set; } = CurrentVersion;

    // First Time User
    public bool IsFirstTimeUser { get; set; } = true;

    // Interface
    public OpenBrioBehavior OpenBrioBehavior { get; set; } = OpenBrioBehavior.OnGPoseEnter;
    public bool ShowInCutscene { get; set; } = false;
    public bool ShowWhenUIHidden { get; set; } = false;

    // Hooks
    public ApplyNPCHack ApplyNPCHack { get; set; } = ApplyNPCHack.InGPose;
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