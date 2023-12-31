using Brio.Resources;
using OneOf.Types;
using OneOf;
using Lumina.Excel.GeneratedSheets;

namespace Brio.Game.Types;

[GenerateOneOf]
internal partial class DyeUnion : OneOfBase<Stain, None>
{
    public static implicit operator DyeUnion(DyeId dyeId)
    {
        if (dyeId.Id != 0 && GameDataProvider.Instance.Stains.TryGetValue(dyeId.Id, out var dye))
            return new DyeUnion(dye);

        return new None();
    }
}

internal record struct DyeId(byte Id)
{
    public static DyeId None { get; } = new(0);

    public static implicit operator DyeId(DyeUnion dyeUnion) => dyeUnion.Match(
        dyeRow => new DyeId((byte)dyeRow.RowId),
        none => None
    );

    public static implicit operator DyeId(int dye) => new((byte)dye);
    public static implicit operator DyeId(byte dye) => new(dye);
    public static implicit operator byte(DyeId id) => id.Id;
    public static implicit operator int(DyeId id) => id.Id;
}
