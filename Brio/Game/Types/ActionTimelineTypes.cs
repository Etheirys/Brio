using Brio.Resources;
using Brio.Resources.Sheets;
using Lumina.Excel.Sheets;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
internal partial class ActionTimelineUnion : OneOfBase<BrioActionTimeline, None>
{
    public static implicit operator ActionTimelineUnion(ActionTimelineId actionTimelineId)
    {
        if(actionTimelineId.Id != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(actionTimelineId.Id, out var timeline))
            return timeline;

        return new None();
    }
}

internal record struct ActionTimelineId(ushort Id)
{
    public static ActionTimelineId None { get; } = new(0);

    public static implicit operator ActionTimelineId(ActionTimelineUnion union) => union.Match(
        row => new ActionTimelineId((ushort)row.RowId),
        none => None
    );

    public static implicit operator ActionTimelineId(ushort id) => new(id);
}
