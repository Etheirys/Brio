using Brio.Core;
using Brio.Resources;
using System;
using System.IO;
using System.Text;

namespace Brio.Files;

[FileType("Mare Character Data", "Images.FileIcon_Mcdf.png", ".mcdf")]
internal class MareCharacterDataFile
{
    public string Description { get; set; } = string.Empty;

    /*public static MareCharacterDataFile Load(string filePath)
    {
        using var unwrapped = File.OpenRead(filePath);
        using var lz4Stream = new LZ4Stream(unwrapped, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression);
        using var reader = new BinaryReader(lz4Stream);

        var chars = new string(reader.ReadChars(4));
        if(!string.Equals(chars, "MCDF", StringComparison.Ordinal))
            throw new InvalidDataException("Not a Mare Chara File");

        var version = reader.ReadByte();
        if (version != 1)
            throw new InvalidDataException("Unsupported Mara Chara file version");

        var dataLength = reader.ReadInt32();
        byte[] data = reader.ReadBytes(dataLength);
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<MareCharacterDataFile>(json);
    }*/
}
