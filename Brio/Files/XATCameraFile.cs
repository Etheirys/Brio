using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Brio.Files;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public record CameraKeyframe(int Frame, Vector3 Position, Quaternion Rotation, float FoV);

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public partial class XATCameraFile
{
    public List<CameraKeyframe> CameraFrames { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public XATCameraFile() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public XATCameraFile(BinaryReader reader)
    {
        // Header
        string fileMagic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if(fileMagic != "XCP1")
        {
            throw new Exception("Unknown file type");
        }

        // Camera
        CameraFrames = [];

        int keyframeCount = (int)reader.ReadUInt32();
        for(int i = 0; i < keyframeCount - 1; i++)
        {
            float xPos = reader.ReadSingle();
            float yPos = reader.ReadSingle();
            float zPos = reader.ReadSingle();
            Vector3 position = new(xPos, yPos, zPos);

            float xRot = reader.ReadSingle();
            float yRot = reader.ReadSingle();
            float zRot = reader.ReadSingle();
            float wRot = reader.ReadSingle();
            Quaternion rotation = new(xRot, yRot, zRot, wRot);

            float fov = reader.ReadSingle();

            CameraFrames.Add(new(i, position, rotation, fov));
        }

        CameraFrames = [.. CameraFrames.OrderBy(x => x.Frame)];
    }
}
