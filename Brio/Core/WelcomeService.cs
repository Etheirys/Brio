using Brio.Config;
using Brio.Game.GPose;
using Brio.UI;

namespace Brio.Core;

public class WelcomeService : ServiceBase<WelcomeService>
{
    public override void Start()
    {
        if(ConfigService.Configuration.IsFirstTimeUser)
        {
            UIService.Instance.InfoWindow.IsOpen = true;
            ConfigService.Configuration.IsFirstTimeUser = false;
        }

        if(ConfigService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            UIService.Instance.InfoWindow.IsOpen = true;
            ConfigService.Configuration.PopupKey = Configuration.CurrentPopupKey;
        }

        if(ConfigService.Configuration.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
            UIService.Instance.MainWindow.IsOpen = true;

        if(ConfigService.Configuration.OpenBrioBehavior == OpenBrioBehavior.OnGPoseEnter && GPoseService.Instance.IsInGPose)
            UIService.Instance.MainWindow.IsOpen = true;

        base.Start();
    }
}
