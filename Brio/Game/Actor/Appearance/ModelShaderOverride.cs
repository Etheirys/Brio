using Brio.Game.Actor.Interop;
using System.Numerics;

namespace Brio.Game.Actor.Appearance;

public struct ModelShaderOverride()
{
    public Vector3? SkinColor = null;
    public float? MuscleTone = null;
    public Vector4? MouthColor = null;
    public Vector3? HairColor = null;
    public Vector3? HairHighlight = null;
    public Vector3? LeftEyeColor = null;
    public Vector3? RightEyeColor = null;
    public Vector3? FeatureColor = null;

    public bool ForceShaderUpdate = false;

    public readonly void Apply(ref BrioHuman.ShaderParams shaders)
    {
        shaders.SkinColor = SkinColor ?? shaders.SkinColor;
        shaders.MuscleTone = MuscleTone ?? shaders.MuscleTone;
        shaders.MouthColor = MouthColor ?? shaders.MouthColor;
        shaders.HairColor = HairColor ?? shaders.HairColor;
        shaders.HairHighlight = HairHighlight ?? shaders.HairHighlight;
        shaders.LeftEyeColor = LeftEyeColor ?? shaders.LeftEyeColor;
        shaders.RightEyeColor = RightEyeColor ?? shaders.RightEyeColor;
        shaders.FeatureColor = FeatureColor ?? shaders.FeatureColor;
    }


    public void Reset()
    {
        SkinColor = null;
        MuscleTone = null;
        MouthColor = null;
        HairColor = null;
        HairHighlight = null;
        LeftEyeColor = null;
        RightEyeColor = null;
        FeatureColor = null;
        ForceShaderUpdate = true;
    }


    public readonly bool HasOverride =>
            SkinColor.HasValue ||
            MuscleTone.HasValue ||
            MouthColor.HasValue ||
            HairColor.HasValue ||
            HairHighlight.HasValue ||
            LeftEyeColor.HasValue ||
            RightEyeColor.HasValue ||
            FeatureColor.HasValue;
}
