using Lumina.Excel.Sheets;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Types;

[GenerateOneOf]
public partial class ItemUnion : OneOfBase<Item, None>
{
}
