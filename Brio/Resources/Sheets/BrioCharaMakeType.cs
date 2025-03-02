
//
// Some code in this file was generated and is from the Lumina.Excel project (https://github.com/NotAdam/Lumina.Excel)
// (Lumina.Excel.Sheets.CharaMakeType)
//

using Brio.Game.Actor.Appearance;
using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Sheets;

[Sheet("CharaMakeType", 0x80D7DB6D)]
public unsafe struct BrioCharaMakeType(ExcelPage page, uint offset, uint row) : IExcelRow<BrioCharaMakeType>
{
    public const int MenuCount = 28;
    public const int SubMenuParamCount = 100;
    public const int SubMenuGraphicCount = 10;
    public const int VoiceCount = 12;
    public const int FaceCount = 8;
    public const int FaceFeatureCount = 7;

    public readonly uint RowId => row;

    public readonly Collection<CharaMakeStructStruct> CharaMakeStruct => new(page, offset, offset, &CharaMakeStructCtor, 28);
    public readonly Collection<byte> VoiceStruct => new(page, offset, offset, &VoiceStructCtor, 12);
    public readonly Collection<FacialFeatureOptionStruct> FacialFeatureOption => new(page, offset, offset, &FacialFeatureOptionCtor, 8);
    public readonly Collection<EquipmentStruct> Equipment => new(page, offset, offset, &EquipmentCtor, 3);
    public readonly RowRef<Race> Race => new(page.Module, (uint)page.ReadInt32(offset + 12392), page.Language);
    public readonly RowRef<Tribe> Tribe => new(page.Module, (uint)page.ReadInt32(offset + 12396), page.Language);
    public readonly sbyte Gender => page.ReadInt8(offset + 12400);

    private static CharaMakeStructStruct CharaMakeStructCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + i * 428);
    private static byte VoiceStructCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt8(offset + 11984 + i);
    private static FacialFeatureOptionStruct FacialFeatureOptionCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + 11996 + i * 28);
    private static EquipmentStruct EquipmentCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => new(page, parentOffset, offset + 12224 + i * 56);

    public readonly struct CharaMakeStructStruct(ExcelPage page, uint parentOffset, uint offset)
    {
        public readonly RowRef<Lobby> Menu => new(page.Module, page.ReadUInt32(offset), page.Language);
        public readonly uint SubMenuMask => page.ReadUInt32(offset + 4);
        public readonly uint Customize => page.ReadUInt32(offset + 8);
        public readonly Collection<uint> SubMenuParam => new(page, parentOffset, offset, &SubMenuParamCtor, 100);
        public readonly byte InitVal => page.ReadUInt8(offset + 412);
        public readonly byte SubMenuType => page.ReadUInt8(offset + 413);
        public readonly byte SubMenuNum => page.ReadUInt8(offset + 414);
        public readonly byte LookAt => page.ReadUInt8(offset + 415);
        public readonly Collection<byte> SubMenuGraphic => new(page, parentOffset, offset, &SubMenuGraphicCtor, 10);

        private static uint SubMenuParamCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt32(offset + 12 + i * 4);
        private static byte SubMenuGraphicCtor(ExcelPage page, uint parentOffset, uint offset, uint i) => page.ReadUInt8(offset + 416 + i);
    }

    public readonly struct FacialFeatureOptionStruct
    {
        public FacialFeatureOptionStruct(ExcelPage page, uint parentOffset, uint offset)
        {
            Options = new int[FaceFeatureCount];
            for(int i = 0; i < FaceFeatureCount; ++i)
            {
                Options[i] = page.ReadInt32((nuint)(offset + i * 4));
            }

        }
        public readonly int[] Options;
    }

#pragma warning disable CS9113 // Parameter is unread.
    public readonly struct EquipmentStruct(ExcelPage page, uint parentOffset, uint offset)
#pragma warning restore CS9113 // Parameter is unread.
    {
        public readonly ulong Helmet => page.ReadUInt64(offset);
        public readonly ulong Top => page.ReadUInt64(offset + 8);
        public readonly ulong Gloves => page.ReadUInt64(offset + 16);
        public readonly ulong Legs => page.ReadUInt64(offset + 24);
        public readonly ulong Shoes => page.ReadUInt64(offset + 32);
        public readonly ulong Weapon => page.ReadUInt64(offset + 40);
        public readonly ulong SubWeapon => page.ReadUInt64(offset + 48);
    }

    static BrioCharaMakeType IExcelRow<BrioCharaMakeType>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);

    public static MenuCollection BuildMenus(ActorAppearance appearance)
    {
        var menus = new List<Menu>();

        var CharaMakeTypes = GameDataProvider.Instance.DataManager.GetExcelSheet<BrioCharaMakeType>(name: "CharaMakeType").
            Where(x => x.Gender == (sbyte)appearance.Customize.Gender && x.Race.RowId == (uint)appearance.Customize.Race).
            First();

        for(uint i = 0; i < CharaMakeTypes.CharaMakeStruct.Count; ++i)
        {
            var firstChar = CharaMakeTypes.CharaMakeStruct[(int)i];

            var title = firstChar.Menu.ValueNullable?.Text.ExtractText() ?? "Unknown";
            var menuType = (MenuType)firstChar.SubMenuType;
            var subMenuNum = firstChar.SubMenuNum;
            var subMenuMask = firstChar.SubMenuMask;
            var customizeIndex = (CustomizeIndex)firstChar.Customize;
            var initialValue = firstChar.InitVal;

            var subParams = new int[subMenuNum];
            var subGraphics = new byte[SubMenuGraphicCount];

            int[,] FacialFeatures = new int[FaceCount, FaceFeatureCount];

            for(int y = 0; y < FaceCount; ++y)
            {
                var faceOption = CharaMakeTypes.FacialFeatureOption[y];
                for(int x = 0; x < faceOption.Options.Length; ++x)
                {
                    FacialFeatures[y, x] = faceOption.Options[x];
                }
            }

            for(int x = 0; x < subMenuNum; ++x)
            {
                if(x >= SubMenuParamCount)
                {
                    subParams[x] = 0;
                    continue;
                }

                subParams[x] = (int)firstChar.SubMenuParam[x];
            }

            menus.Add(new Menu(i, CharaMakeTypes.RowId, title,
                CharaMakeTypes.Race.IsValid ? (Races)CharaMakeTypes.Race.Value.RowId : 0,
                CharaMakeTypes.Tribe.IsValid ? (Tribes)CharaMakeTypes.Tribe.Value.RowId : 0,
                (Genders)CharaMakeTypes.Gender, menuType, subMenuMask, customizeIndex,
                initialValue, subParams, subGraphics, [.. CharaMakeTypes.VoiceStruct], FacialFeatures));


        }

        return new MenuCollection([.. menus]);
    }

    public class MenuCollection(Menu[] menus)
    {
        public Menu[] Menus { get; } = menus;

        public Menu? GetMenuForCustomize(CustomizeIndex index)
        {
            return Menus.FirstOrDefault(x => x.CustomizeIndex == index);
        }

        public Menu[] GetMenusForCustomize(CustomizeIndex index)
        {
            return Menus.Where(x => x.CustomizeIndex == index).ToArray();
        }

        public MenuType GetMenuTypeForCustomize(CustomizeIndex index)
        {
            return GetMenuForCustomize(index)?.Type ?? MenuType.Unknown;
        }
    }

    public record class Menu(uint MenuId,
        uint CharaMakeRow, string Title,
        Races Race, Tribes Tribe, Genders
        Gender, MenuType Type, uint MenuMask,
        CustomizeIndex CustomizeIndex, byte InitialValue,
        int[] SubParams, byte[] SubGraphics, byte[] Voices,
        int[,] FacialFeatures);

    public enum MenuType : byte
    {
        List = 0,
        ItemSelect = 1,
        Color = 2,
        Unknown3 = 3,
        MultiItemSelect = 4,
        Numerical = 5,
        Unknown,
    }
}
