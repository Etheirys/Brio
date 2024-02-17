using Brio.Capabilities.Posing;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Posing;
using Brio.Game.Posing.Skeletons;
using EasyTcp4;
using EasyTcp4.ServerUtils;
using MessagePack;
using System;
using System.Threading.Tasks;

namespace Brio.Remote;
internal class RemoteService : IDisposable
{
    public const int SyncMs = 33;

    private readonly EntityManager _entityManager;
    private EasyTcpServer? _server;

    public RemoteService(EntityManager entityManager)
    {
        _entityManager = entityManager;

        StartServer();
    }

    public bool StartServer()
    {
        if(_server != null)
            throw new Exception("Attempt to start IPC server while it is already running");

        _server = new();
        _server.EnableServerKeepAlive();
        _server.OnDataReceive += OnDataReceived;
        _server.OnError += (s, e) => Brio.Log.Error(e, "Remote error");
        _server.OnConnect += (s, e) => Brio.Log.Info("Remote client connected");
        _server.OnDisconnect += (s, e) => Brio.Log.Info("Remote client disconnected");

        _server.Start(Configuration.Port);

        Task.Run(Synchronizer);

        return _server.IsRunning;
    }

    public void Dispose()
    {
        _server?.Dispose();
        _server = null;
    }

    public void Send(object obj)
    {
        byte[] data = MessagePackSerializer.Typeless.Serialize(obj);
        _server.SendAll(data);
    }

    private void OnDataReceived(object? sender, Message e)
    {
        object? obj = MessagePackSerializer.Typeless.Deserialize(e.Data);
    }

    private async Task Synchronizer()
    {
        while(_server != null)
        {
            await Task.Delay(SyncMs);

            if (_entityManager.SelectedEntity is ActorEntity actor)
            {
                PosingCapability? posing;
                if (actor.TryGetCapability<PosingCapability>(out posing))
                {
                    SynchronizePosing(posing);
                }
            }
        }
    }

    private void SynchronizePosing(PosingCapability posing)
    {
        posing.Selected.Switch(
            bone =>
            {
                Bone? realBone = posing.SkeletonPosing.GetBone(bone);
                if(realBone != null && realBone.Skeleton.IsValid)
                {
                    BoneMessage boneMessage = new();
                    boneMessage.FromBone(realBone);
                    Send(boneMessage);
                }
            },
            _ =>
            {
                // Model
            },
            _ =>
            {
                // Model
            }
        );
    }
}
