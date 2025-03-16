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

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (float)(System.Math.PI / 180);
    }

    public static float RadiansToDegrees(float radians)
    {
        return radians * (float)(180 / System.Math.PI);
    }
}
