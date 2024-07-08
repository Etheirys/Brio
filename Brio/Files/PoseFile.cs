using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Files.Converters;
using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Files;

internal class PoseFileInfo : AppliableActorFileInfoBase<PoseFile>
{
    public override string Name => "Pose File";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Pose.png");
    public override string Extension => ".pose";

    public PoseFileInfo(EntityManager entityManager)
    : base(entityManager)
    {
    }

    protected override void Apply(PoseFile file, ActorEntity actor, bool asExpression)
    {
        PosingCapability? capability;
        if(actor.TryGetCapability<PosingCapability>(out capability) && capability != null)
        {
            capability.ImportPose(file, asExpression: asExpression);
        }
    }
}

[Serializable]
internal class PoseFile : JsonDocumentBase
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
