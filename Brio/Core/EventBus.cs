using Brio.UI;

namespace Brio.Core;

public class EventBus
{
    public static EventBus Instance { get; private set; } = null!;

    public EventBus()
    {
        Instance = this;
    }

    public void NotifyError(string message)
    {
        UIManager.Instance.NotifyError(message);
    }
}
