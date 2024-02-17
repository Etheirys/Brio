using MessagePack;

namespace Brio.Remote;

[MessagePackObject]
public class Heartbeat
{
    [Key(00)] public int Count { get; set; }
}
