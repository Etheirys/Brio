using Brio.Core;
using Brio.Files.Converters;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Files;

[FileType("Pose File", "Images.FileIcon_Pose.png", ".pose")]
internal class PoseFile : FileBase
{
    public Bone ModelDifference { get; set; } = Transform.Identity;

    public Dictionary<string, Bone> Bones { get; set; } = [];
    public Dictionary<string, Bone> MainHand { get; set; } = [];
    public Dictionary<string, Bone> OffHand { get; set; } = [];

    public class Bone
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public static implicit operator Transform(Bone bone)
        {
            return new Transform()
            {
                Position = bone.Position,
                Rotation = bone.Rotation,
                Scale = bone.Scale
            };
        }

        public static implicit operator Bone(Transform bone)
        {
            return new Bone()
            {
                Position = bone.Position,
                Rotation = bone.Rotation,
                Scale = bone.Scale
            };
        }
    }

    public void SanitizeBoneNames()
    {
        var newBones = new Dictionary<string, Bone>();
        foreach(var bone in Bones)
        {
            newBones[AnamnesisBoneNameConverter.AnamnesisToGame(bone.Key)] = bone.Value;
        }
        Bones = newBones;
    }
}
