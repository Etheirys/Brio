using MessagePack;

namespace Brio.Remote;

[MessagePackObject]
public class Heartbeat
{
    [Key(0)]
    public int Count { get; set; }
}
