﻿using Brio.Resources;
using Lumina.Excel.Sheets;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
public partial class FacewearUnion : OneOfBase<Glasses, None>
{
    public static implicit operator FacewearUnion(FacewearId facewearId)
    {
        if(facewearId.Id != 0 && GameDataProvider.Instance.Glasses.TryGetValue(facewearId.Id, out var glasses))
            return new FacewearUnion(glasses);

        return new None();
    }
}

public record struct FacewearId(byte Id)
{
    public static FacewearId None { get; } = new(0);

    public static implicit operator FacewearId(DyeUnion dyeUnion) => dyeUnion.Match(
        dyeRow => new FacewearId((byte)dyeRow.RowId),
        none => None
    );

    public static implicit operator FacewearId(int dye) => new((byte)dye);
    public static implicit operator FacewearId(byte dye) => new(dye);
    public static implicit operator byte(FacewearId id) => id.Id;
    public static implicit operator int(FacewearId id) => id.Id;
}
