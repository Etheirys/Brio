using EasyTcp4;
using EasyTcp4.ServerUtils;
using MessagePack;
using System;

namespace Brio.Remote;
public class RemoteService : IDisposable
{
    public const int Port = 1200;

    private EasyTcpServer? _server;

    public RemoteService()
    {
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

        _server.Start(Port);

        return _server.IsRunning;
    }

    public void Dispose()
    {
        _server?.Dispose();
    }

    public void Send(byte[] data)
    {
        _server.SendAll(data);
    }

    private void OnDataReceived(object? sender, Message e)
    {
        object? obj = MessagePackSerializer.Typeless.Deserialize(e.Data);

        Brio.Log.Info($"Received {obj?.GetType()}");
    }
}
