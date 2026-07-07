namespace Brio.Config;

public class TimelineConfiguration
{
    public double PlaybackFramesPerSecond { get; set; } = 60;
    public bool Loop { get; set; } = true;

    public bool OpenWithGPose { get; set; }

    public bool ShowInspector { get; set; } = true;
}
