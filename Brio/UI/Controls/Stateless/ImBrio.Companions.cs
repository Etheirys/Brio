using Brio.Game.Types;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
{
    public static bool BorderedGameIcon(string id, CompanionRowUnion union, bool showText = true, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        var (description, icon) = union.Match(
           companion => ($"{companion.Singular}\n{companion.RowId}\nModel: {companion.Model.RowId}", companion.Icon),
           mount => ($"{mount.Singular}\n{mount.RowId}\nModel: {mount.ModelChara.RowId}", mount.Icon),
           ornament => ($"{ornament.Singular}\n{ornament.RowId}\nModel: {ornament.Model}", ornament.Icon),
           none => ("None", (uint)0)
       );

        bool wasClicked = false;

        if(!showText)
        {
            description = string.Empty;
        }

        var placeholderIcon = union.Match(
                companion => "Images.Companion.png",
                mount => "Images.Mount.png",
                ornament => "Images.Ornament.png",
                none => "Images.Companion.png"
            );

        wasClicked = BorderedGameIcon(id, icon, placeholderIcon, description, flags, size);

        return wasClicked;
    }
}
