using Brio.Remote;
using EasyTcp4;
using EasyTcp4.ClientUtils;
using EasyTcp4.ClientUtils.Async;
using MessagePack;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace WpfRemote;

internal class RemoteService
{
    public const int Port = Brio.Remote.RemoteService.Port;

    private EasyTcpClient? _client;
    private int _heartbeatIndex = 0;

    public RemoteService()
    {
        Task.Run(StartClient);
    }

    public async Task<bool> StartClient()
    {
        if (_client != null)
            throw new Exception("Attempt to start remote client while it is already running");

        _client = new();
        _client.OnError += (s, e) => Log.Error(e, "IPC error");
        _client.OnDataReceive += this.OnDataReceived;

        bool success = await _client.ConnectAsync(IPAddress.Loopback, Port);

        if (success)
            _ = Task.Run(HeartbeatTask);

        return success;
    }

    public void Send(object obj)
    {
        byte[] data = MessagePackSerializer.Typeless.Serialize(obj);
        _client.Send(data);
    }

    private void OnDataReceived(object? sender, Message e)
    {
    }

    private async Task HeartbeatTask()
    {
        while(Application.Current != null && _client.IsConnected())
        {
            Heartbeat hb = new();
            hb.Count = _heartbeatIndex;
            Send(hb);
            
            await Task.Delay(1000);
        }
    }
}

