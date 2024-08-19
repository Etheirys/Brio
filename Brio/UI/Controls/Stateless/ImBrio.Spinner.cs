using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static void Spinner(ref float angle, float speed = 3.5f)
    {
        angle += ImGui.GetIO().DeltaTime * speed;

        IDalamudTextureWrap img = ResourceProvider.Instance.GetResourceImage("Images.Spinner.png");
        ImageRotated(img, angle);

        if(angle > 360)
        {
            angle = 0;
        }
    }
}
