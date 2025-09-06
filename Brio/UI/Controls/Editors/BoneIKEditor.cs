using Brio.Capabilities.Posing;
using Brio.Game.Posing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Controls.Editors;
public class BoneIKEditor
{
    public static void Draw(BonePoseInfo poseInfo, PosingCapability posing)
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


            string solverType = ik.SolverOptions.Match(_ => "CCD", _ => "Two Joint");
            using(var combo = ImRaii.Combo("Solver", solverType))
            {
                if(combo.Success)
                {
                    if(ImGui.Selectable("CCD"))
                    {
                        ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, false).SolverOptions;
                        didChange |= true;
                    }

                    if(BoneIKInfo.CanUseJoint(poseInfo.Name))
                    {
                        if(ImGui.Selectable("Two Joint"))
                        {
                            ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, true).SolverOptions;
                            didChange |= true;
                        }
                    }
                }
            }

            ik.SolverOptions.Switch(
                ccd =>
                {
                    if(ImGui.SliderInt("Depth", ref ccd.Depth, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }

                    if(ImGui.SliderInt("Iterations", ref ccd.Iterations, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }
                },
                twoJoint =>
                {
                }
             );
        }

        if(didChange)
        {
            poseInfo.DefaultIK = ik;
        }
    }
}
