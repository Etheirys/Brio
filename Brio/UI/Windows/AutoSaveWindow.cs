using Brio.Config;
using Brio.Game.GPose;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;

namespace Brio.UI.Windows;
public class AutoSaveWindow : Window, IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly GPoseService _gPoseService;


    public AutoSaveWindow(ConfigurationService configurationService, GPoseService gPoseService) : base($"Auto Saves###brio_autosaves_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Namespace = "brio_autosaves_window";

        this.AllowClickthrough = false;
        this.AllowPinning = false;
        this.ForceMainWindow = true;

        _configurationService = configurationService;
        _gPoseService = gPoseService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(600, 400),
            MaximumSize = new(785, 435)
        };
        this.SizeConstraints = constraints;
        _gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    public override void Draw()
    {

    }

    private void GPoseService_OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            this.IsOpen = false;
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}
