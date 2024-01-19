using Brio.Resources;
using OneOf;
using OneOf.Types;

namespace Brio.Game.Posing;

[GenerateOneOf]
internal partial class PosingSelectionType : OneOfBase<BonePoseInfoId, ModelTransformSelection, None>
{
    public static None None { get; } = new None();
    public static ModelTransformSelection ModelTransform { get; } = new();

    public string DisplayName => Match(
        bone => Localize.Get($"bones.{bone.BoneName}", bone.BoneName),
        model => "Model Transform",
        none => "None"
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

        if(obj is PosingSelectionType other)
            return UniqueId.Equals(other.UniqueId);

        if(obj is None && Value is None)
            return true;

        if(obj is ModelTransformSelection && Value is ModelTransformSelection)
            return true;

        if(obj is BonePoseInfoId bone && obj is BonePoseInfoId otherBone)
            return bone.Equals(otherBone);

        return base.Equals(obj);
    }

    public static bool operator ==(PosingSelectionType? left, PosingSelectionType? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(PosingSelectionType? left, PosingSelectionType? right) => !(left == right);

    public override int GetHashCode() => UniqueId.GetHashCode();
}

internal record struct ModelTransformSelection();