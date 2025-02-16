using Brio.Game.Actor.Appearance;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Sheets;

[Sheet("HairMakeType")]
public struct BrioHairMakeType : IExcelRow<BrioHairMakeType>
{
    public const int EntryCount = 100;

    public BrioHairMakeType()
    {
    }

    public uint RowId { get; private set; }

    public RowRef<Race> Race { get; private set; }
    public RowRef<Tribe> Tribe { get; private set; }
    public Genders Gender { get; private set; }

    public RowRef<CharaMakeCustomize>[] HairStyles = new RowRef<CharaMakeCustomize>[EntryCount];
    public RowRef<CharaMakeCustomize>[] FacePaints = new RowRef<CharaMakeCustomize>[EntryCount];

    public static BrioHairMakeType Create(ExcelPage page, uint offset, uint row)
    {
        var brioHairMakeType = new BrioHairMakeType
        {
            RowId = row,

            Race = new RowRef<Race>(page.Module, (uint)page.ReadInt32(offset + 4076), page.Language),
            Tribe = new RowRef<Tribe>(page.Module, (uint)page.ReadInt32(offset + 4080), page.Language),
            Gender = (Genders)(uint)page.ReadInt8(offset + 4084)
        };

        for(int i = 0; i < EntryCount; i++)
            brioHairMakeType.HairStyles[i] = new RowRef<CharaMakeCustomize>(page.Module, page.ReadUInt32((nuint)(offset + 0xC + (i * 4))), page.Language);

        for(int i = 0; i < EntryCount; i++)
            brioHairMakeType.FacePaints[i] = new RowRef<CharaMakeCustomize>(page.Module, page.ReadUInt32((nuint)(offset + 0xBC0 + (i * 4))), page.Language);

        return brioHairMakeType;
    }

    public static IEnumerable<CharaMakeCustomize> GetHairStyles(ActorCustomize customize)
    {
        var HairMakeType = GameDataProvider.Instance.DataManager.GetExcelSheet<BrioHairMakeType>().Where(x => x.Gender == customize.Gender && x.Race.RowId == (uint)customize.Race && x.Tribe.RowId == (uint)customize.Tribe);

        foreach(var item in HairMakeType)
        {
            foreach(var item2 in item.HairStyles)
            {
                if(item2.IsValid)
                    yield return item2.Value;
            }
        }
    }

    public static IEnumerable<CharaMakeCustomize> GetFacePaints(ActorCustomize customize)
    {
        var FacePaints = GameDataProvider.Instance.DataManager.GetExcelSheet<BrioHairMakeType>().Where(x => x.Gender == customize.Gender && x.Race.RowId == (uint)customize.Race && x.Tribe.RowId == (uint)customize.Tribe);

        foreach(var item in FacePaints)
        {
            foreach(var item2 in item.FacePaints)
            {
                if(item2.IsValid)
                    yield return item2.Value;
            }
        }
    }
}
