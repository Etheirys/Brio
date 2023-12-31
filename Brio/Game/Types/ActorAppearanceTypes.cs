using Brio.Game.Actor.Appearance;
using Lumina.Excel.GeneratedSheets;
using OneOf;
using OneOf.Types;
using Companion = Lumina.Excel.GeneratedSheets.Companion;
using Ornament = Lumina.Excel.GeneratedSheets.Ornament;

namespace Brio.Game.Types;

[GenerateOneOf]
internal partial class ActorAppearanceUnion : OneOfBase<BNpcBase, ENpcBase, Mount, Companion, Ornament, None>
{
    public static implicit operator ActorAppearance(ActorAppearanceUnion union) => union.Match(
        bnpc => ActorAppearance.FromBNpc(bnpc),
        enpc => ActorAppearance.FromENpc(enpc),
        mount => ActorAppearance.FromModelChara((int)mount.ModelChara.Row),
        companion => ActorAppearance.FromModelChara((int)companion.Model.Row),
        ornament => ActorAppearance.FromModelChara(ornament.Model),
        none => new ActorAppearance()
    );
}

