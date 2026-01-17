using Blake3;
using System;
using System.IO;

namespace Brio.Core;

public static class BLAKE3
{
    public const int HashLengthHex = 64;

    public static string HashFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return HashStream(stream);
    }
    public static string HashStream(Stream stream)
    {
        using var hasher = Hasher.New();
        Span<byte> buffer = stackalloc byte[71526];
        int bytesRead;

        while((bytesRead = stream.Read(buffer)) > 0)
        {
            hasher.Update(buffer[..bytesRead]);
        }

        return hasher.Finalize().ToString().ToUpperInvariant();
    }

    public static string HashBytes(byte[] data)
    {
        using var hasher = Hasher.New();
        hasher.Update(data);
        return hasher.Finalize().ToString().ToUpperInvariant();
    }

    public static string HashBytes(ReadOnlySpan<byte> data)
    {
        using var hasher = Hasher.New();
        hasher.Update(data);
        return hasher.Finalize().ToString().ToUpperInvariant();
    }
}

