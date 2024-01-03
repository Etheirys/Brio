using Brio.Game.Posing;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace Brio.UI.Controls.Editors;
internal class BoneIKEditor
{
    public static void Draw(BonePoseInfo poseInfo)
    {
        bool didChange = false;

        var ik = poseInfo.DefaultIK;

        if(ImGui.Checkbox("Enabled", ref ik.Enabled))
        {
            didChange |= true;
        }

        using(ImRaii.Disabled(!ik.Enabled))
        {

            if(ImGui.Checkbox("Enforce Constraints", ref ik.EnforceConstraints))
            {
                didChange |= true;
            }

            if(ImGui.SliderInt("Depth", ref ik.Depth, 1, 20))
            {
                didChange |= true;
            }

            if(ImGui.SliderInt("Iterations", ref ik.Iterations, 1, 20))
            {
                didChange |= true;
            }
        }

        if(didChange)
        {
            poseInfo.DefaultIK = ik;
        }
    }
}
