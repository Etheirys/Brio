using Brio.Game.Posing.Skeletons;
using MessagePack;

namespace Brio.Remote;

[MessagePackObject]
public class BoneMessage
{
    [Key(00)] public string? Name { get; set; }
    [Key(01)] public string? DisplayName { get; set; }
    [Key(02)] public float PositionX { get; set; }
    [Key(03)] public float PositionY { get; set; }
    [Key(04)] public float PositionZ { get; set; }
    [Key(05)] public float ScaleX { get; set; }
    [Key(06)] public float ScaleY { get; set; }
    [Key(07)] public float ScaleZ { get; set; }
    [Key(08)] public float RotationX { get; set; }
    [Key(09)] public float RotationY { get; set; }
    [Key(10)] public float RotationZ { get; set; }
    [Key(11)] public float RotationW { get; set; }

    internal void FromBone(Bone bone)
    {
        this.PositionX = bone.LastTransform.Position.X;
        this.PositionY = bone.LastTransform.Position.Y;
        this.PositionZ = bone.LastTransform.Position.Z;
        this.ScaleX = bone.LastTransform.Scale.X;
        this.ScaleY = bone.LastTransform.Scale.Y;
        this.ScaleZ = bone.LastTransform.Scale.Z;
        this.RotationX = bone.LastTransform.Rotation.X;
        this.RotationY = bone.LastTransform.Rotation.Y;
        this.RotationZ = bone.LastTransform.Rotation.Z;
        this.RotationW = bone.LastTransform.Rotation.W;
    }
}
