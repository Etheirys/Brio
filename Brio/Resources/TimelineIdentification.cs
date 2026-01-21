using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System.Collections.Generic;

namespace Brio.Resources;

public class TimelineIdentification
{
    private readonly IdentificationDictionary<uint> facialExpressions = [];
    private readonly IdentificationDictionary<uint> basePoses = [];
    private readonly IdentificationDictionary<uint> upperBody = [];

    private IDataManager _dataManager;

    private class IdentificationDictionary<T> : Dictionary<T, string> where T : notnull
    {
        public void Copy(T source, params T[] targets)
        {
            if(TryGetValue(source, out var value))
            {
                foreach(var target in targets)
                {
                    TryAdd(target, value);
                }
            }
        }
    }

    public TimelineIdentification(IDataManager dataManager)
    {
        _dataManager = dataManager;

        // Standing Idles
        basePoses.TryAdd(0, "No Pose");
        basePoses.TryAdd(3, "Standing Idle Pose #1");
        basePoses.TryAdd(3124, "Standing Idle Pose #2");
        basePoses.TryAdd(3126, "Standing Idle Pose #3");
        basePoses.TryAdd(3182, "Standing Idle Pose #4");
        basePoses.TryAdd(3184, "Standing Idle Pose #5");
        basePoses.TryAdd(7405, "Standing Idle Pose #6");
        basePoses.TryAdd(7407, "Standing Idle Pose #7");

        // Ground Sit
        basePoses.TryAdd(654, "Ground Sit Pose #1");
        basePoses.TryAdd(3136, "Ground Sit Pose #2");
        basePoses.TryAdd(3138, "Ground Sit Pose #3");
        basePoses.TryAdd(3171, "Ground Sit Pose #4");

        // Chair Sit
        basePoses.TryAdd(643, "Chair Sit Pose #1");
        basePoses.TryAdd(3132, "Chair Sit Pose #2");
        basePoses.TryAdd(3134, "Chair Sit Pose #3");
        basePoses.TryAdd(8002, "Chair Sit Pose #4");
        basePoses.TryAdd(8004, "Chair Sit Pose #5");

        // Sleeping
        basePoses.TryAdd(585, "Sleeping Pose #1");
        basePoses.TryAdd(3140, "Sleeping Pose #2");
        basePoses.TryAdd(3142, "Sleeping Pose #3");

        // Parasol
        basePoses.TryAdd(7367, "Parasol Idle Pose #1");
        basePoses.TryAdd(8063, "Parasol Idle Pose #2");
        basePoses.TryAdd(8066, "Parasol Idle Pose #3");
        basePoses.TryAdd(8068, "Parasol Idle Pose #4");

        // Walking
        basePoses.TryAdd(13, "Walking Forward");
        basePoses.TryAdd(14, "Walking Left");
        basePoses.TryAdd(15, "Walking Right");
        basePoses.TryAdd(16, "Walking Backward");

        // Running
        basePoses.TryAdd(22, "Running Forward");
        basePoses.TryAdd(23, "Running Left");
        basePoses.TryAdd(24, "Running Right");
        basePoses.TryAdd(25, "Running Start");
        basePoses.TryAdd(26, "Running Start [BR]");
        basePoses.TryAdd(27, "Running Start [BL]");

        // Other
        basePoses.TryAdd(73, "Dead");


        basePoses.TryAdd(3165, "Kneeling Down");
        basePoses.TryAdd(3166, "Kneeling Down [M]");
        basePoses.TryAdd(3167, "Kneeling Down [H]");
        basePoses.TryAdd(3168, "Standing on Tip Toes");
        basePoses.TryAdd(3186, "Target Same Height");

        facialExpressions.TryAdd(0, "No Expression");
        foreach(var emote in dataManager.GetExcelSheet<Emote>())
        {
            if(emote.EmoteCategory.RowId == 3)
            {
                facialExpressions.TryAdd(emote.ActionTimeline[0].RowId, emote.Name.ExtractText());
                continue;
            }


            foreach(var tl in emote.ActionTimeline)
            {
                if(tl.RowId == 0 || !tl.IsValid) continue;

                if(tl.Value.Stance == 0)
                {
                    basePoses.TryAdd(tl.RowId, emote.Name.ExtractText());
                }
                else if(tl.Value.Stance == 1)
                {
                    upperBody.TryAdd(tl.RowId, emote.Name.ExtractText());
                }
            }
        }


    }

    public string GetExpressionName(ushort timeline)
    {
        if(facialExpressions.TryGetValue(timeline, out var name)) return name;

        if(_dataManager.GetExcelSheet<ActionTimeline>().TryGetRow(timeline, out var row))
        {
            return row.Key.ExtractText();
        }

        return GetKey(timeline);
    }

    private string GetKey(uint timeline)
    {
        if(_dataManager.GetExcelSheet<ActionTimeline>().TryGetRow(timeline, out var row))
        {
            return row.Key.ExtractText();
        }

        return $"Timeline#{timeline}";
    }

    public string GetBodyPoseName((ushort timeline, ushort upperBodyTimeline) t) => GetBodyPoseName(t.timeline, t.upperBodyTimeline);

    public string GetBodyPoseName(ushort timeline, ushort upperBodyTimeline)
    {
        if(!basePoses.TryGetValue(timeline, out var name)) name = GetKey(timeline);
        if(upperBodyTimeline == 0) return name;
        if(upperBody.TryGetValue(upperBodyTimeline, out var upperBodyName)) return $"{upperBodyName} while {name}";
        return $"{GetKey(upperBodyTimeline)} while {name}";
    }
}
