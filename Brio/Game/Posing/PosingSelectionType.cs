using Brio.Resources;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Posing;

[GenerateOneOf]
public partial class PosingSelectionType : OneOfBase<BonePoseInfoId, ModelTransformSelection, None>
{
    public static None None { get; } = new None();
    public static ModelTransformSelection ModelTransform { get; } = new();

    public string DisplayName => Match(
        bone => Localize.Get($"bones.{bone.BoneName}", bone.BoneName),
        model => "Model Transform",
        none => "Model Transform"
    );

    public string Subtitle => Match(
       bone => bone.BoneName,
       model => "",
       none => ""
   );

    public string UniqueId => Match(
        bone => $"selection_{bone}",
        model => "selection_model",
        none => "selection_none"
    );

    public override string ToString() => UniqueId;

    public override bool Equals(object? obj)
    {
        if(obj is null)
            return false;

        return obj is PosingSelectionType other
            ? UniqueId.Equals(other.UniqueId)
            : obj is None && Value is None
            ? true
            : obj is ModelTransformSelection && Value is ModelTransformSelection
            ? true
            : obj is BonePoseInfoId bone and BonePoseInfoId otherBone 
            ? bone.Equals(otherBone) 
            : base.Equals(obj);
    }

    public static bool operator ==(PosingSelectionType? left, PosingSelectionType? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(PosingSelectionType? left, PosingSelectionType? right) => !(left == right);

    public override int GetHashCode() => UniqueId.GetHashCode();
}

public record struct ModelTransformSelection();
