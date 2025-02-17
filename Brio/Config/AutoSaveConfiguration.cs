namespace Brio.Config;

public class AutoSaveConfiguration
{
    public bool AutoSaveSystemEnabled { get; set; } = true;

    public int AutoSaveInterval { get; set; } = 60;
    public int MaxAutoSaves { get; set; } = 8;

    public bool CleanAutoSaveOnLeavingGpose { get; set; } = false;

}
