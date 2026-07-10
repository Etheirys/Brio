using Brio.Entities.Core;
using Brio.UI;
using Dalamud.Interface;
using System;

namespace Brio.Entities.Vivacity;

public class TimelineEntity(IServiceProvider provider) : Entity(FixedId, provider)
{
    public const string FixedId = "timeline_entity";

    public override string FriendlyName => "Vivacity Timeline";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Timeline;

    public override EntityFlags Flags => EntityFlags.DisableSelection;

    public override bool IsAttached => true;

    public override void OnSelected()
    {
        UIManager.Instance.ToggleTimelineWindow();
    }
}
