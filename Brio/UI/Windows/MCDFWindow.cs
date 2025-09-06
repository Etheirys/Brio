using Brio.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Brio.UI.Windows;

public class MCDFWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public MCDFWindow(ConfigurationService configurationService) : base($"{Brio.Name} MCDF ###brio_mcdf_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Namespace = "brio_mcdf_window";

        this.AllowClickthrough = false;
        this.AllowPinning = false;
        this.ForceMainWindow = true;

        _configurationService = configurationService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(600, 400),
            MaximumSize = new(785, 435)
        };
        this.SizeConstraints = constraints;
    }

    public override void Draw()
    {

    }
}
