using Brio.Resources;
using Lumina.Excel.GeneratedSheets;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
internal partial class CompanionRowUnion : OneOfBase<Companion, Mount, Ornament, None>
{
    public static implicit operator CompanionRowUnion(CompanionContainer container)
    {
        if(container.Id == 0)
            return new None();

        return container.Kind switch
        {
            CompanionKind.Companion => GameDataProvider.Instance.Companions[container.Id],
            CompanionKind.Mount => GameDataProvider.Instance.Mounts[container.Id],
            CompanionKind.Ornament => GameDataProvider.Instance.Ornaments[container.Id],
            CompanionKind.None => new None(),
            _ => new None()
        };
    }
}


internal enum CompanionKind
{
    Companion,
    Mount,
    Ornament,
    None
}

internal record struct CompanionContainer(CompanionKind Kind, ushort Id)
{
    public static CompanionContainer None { get; } = new CompanionContainer(CompanionKind.None, 0);

    public static implicit operator CompanionContainer(CompanionRowUnion row) => row.Match(
        companion => new CompanionContainer(CompanionKind.Companion, (ushort)companion.RowId),
        mount => new CompanionContainer(CompanionKind.Mount, (ushort)mount.RowId),
        ornament => new CompanionContainer(CompanionKind.Ornament, (ushort)ornament.RowId),
        none => new CompanionContainer(CompanionKind.None, (ushort)0)
    );
}