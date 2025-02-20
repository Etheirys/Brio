using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Interop;

namespace Brio.Core;


public static class BrioUtilities
{
    // Imports Custom Colors from Chara files into Brio Shaders
    public static void ImportShadersFromFile(ref ModelShaderOverride modelShaderOverride, BrioHuman.ShaderParams shaderParams)
    {
        modelShaderOverride.SkinColor = shaderParams.SkinColor;
        modelShaderOverride.SkinGloss = shaderParams.SkinGloss;
        modelShaderOverride.MuscleTone = shaderParams.MuscleTone;
        modelShaderOverride.MouthColor = shaderParams.MouthColor;
        modelShaderOverride.HairColor = shaderParams.HairColor;
        modelShaderOverride.HairGloss = shaderParams.HairGloss;
        modelShaderOverride.HairHighlight = shaderParams.HairHighlight;
        modelShaderOverride.LeftEyeColor = shaderParams.LeftEyeColor;
        modelShaderOverride.RightEyeColor = shaderParams.RightEyeColor;
        modelShaderOverride.FeatureColor = shaderParams.FeatureColor;
    }
}
