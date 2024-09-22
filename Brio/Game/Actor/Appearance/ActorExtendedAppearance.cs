using System.Numerics;

namespace Brio.Game.Actor.Appearance;

internal struct ActorExtendedAppearance()
{
    public float Transparency = 1.0f;
    public float Wetness = 0.0f;
    public float WetnessDepth = 0.0f;

    public Vector4 CharacterTint = Vector4.One;
    public Vector4 MainHandTint = Vector4.One;
    public Vector4 OffHandTint = Vector4.One;


    //
    // TODO, Brio ignores the following filds

    public float HeightMultiplier = 1.0f;
}
