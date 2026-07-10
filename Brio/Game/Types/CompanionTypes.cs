using Brio.Resources;
using Lumina.Excel.Sheets;
using MessagePack;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
public partial class CompanionRowUnion : OneOfBase<Companion, Mount, Ornament, None>
{
    public static implicit operator CompanionRowUnion(CompanionContainer container)
    {
        if(container.Id == 0)
            return new None();

        return container.Kind switch
        {
            CompanionKind.Companion => GameDataProvider.Instance.GetExcelSheet<Companion>().GetRow(container.Id),
            CompanionKind.Mount => GameDataProvider.Instance.GetExcelSheet<Mount>().GetRow(container.Id),
            CompanionKind.Ornament => GameDataProvider.Instance.GetExcelSheet<Ornament>().GetRow(container.Id),
            CompanionKind.None => new None(),
            _ => new None()
        };
    }
}


public enum CompanionKind
{
    Companion,
    Mount,
    Ornament,
    None
}

[MessagePackObject(keyAsPropertyName: true)]
public record struct CompanionContainer(CompanionKind Kind, ushort Id)
{
    public static CompanionContainer None { get; } = new CompanionContainer(CompanionKind.None, 0);

    public static implicit operator CompanionContainer(CompanionRowUnion row) => row.Match(
        companion => new CompanionContainer(CompanionKind.Companion, (ushort)companion.RowId),
        mount => new CompanionContainer(CompanionKind.Mount, (ushort)mount.RowId),
        ornament => new CompanionContainer(CompanionKind.Ornament, (ushort)ornament.RowId),
        none => new CompanionContainer(CompanionKind.None, (ushort)0)
    );
}
