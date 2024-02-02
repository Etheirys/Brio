using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files.Converters;
using Brio.Library.Sources;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.Files;

internal class PoseFileInfo : JsonDocumentBaseFileInfo<PoseFile>
{
    public override string Name => "Pose File";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Pose.png");
    public override string Extension => ".pose";


    private Task Apply(FileEntry fileEntry, ActorEntity actor)
    {
        PoseFile? file = Load(fileEntry.FilePath) as PoseFile;
        if(file != null)
        {
            PosingCapability? capability;
            if(actor.TryGetCapability<PosingCapability>(out capability) && capability != null)
            {
                capability.ImportPose(file);
            }
        }

        return Task.CompletedTask;
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
