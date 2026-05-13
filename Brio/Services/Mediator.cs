using Brio.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Brio.Services;

public class Mediator() : IDisposable
{
    private readonly Channel<MessageBase> _queue = Channel.CreateUnbounded<MessageBase>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly CancellationTokenSource _loopCTS = new();

    private readonly Lock _addRemoveLock = new();
    private readonly ConcurrentDictionary<Type, SubscriberContainer> _subscribers = new();
    private readonly ConcurrentDictionary<SubscriberAction, DateTime> _lastErrorTime = new();

    private Task? _pumpTask;

    private DiagnosticTracker _perfTracker = new("Mediator", logInterval: 7000, slowFrameThresholdMs: 5.0, slowFrameCooldownFrames: 7000, []);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Brio.Log.Debug("Starting Mediator");
        _pumpTask = Task.Run(PumpAsync, cancellationToken);
        Brio.Log.Info("Started Mediator");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();
        if(!_loopCTS.IsCancellationRequested)
            _loopCTS.Cancel();

        if(_pumpTask is not null)
        {
            try 
            { 
                await _pumpTask.ConfigureAwait(false);
            } catch(Exception) { } // just ignore
        }
    }

    private async Task PumpAsync()
    {
        try
        {
            while(await _queue.Reader.WaitToReadAsync(_loopCTS.Token).ConfigureAwait(false))
            {
                while(_queue.Reader.TryRead(out var message))
                {
                    using(Diagnostics.MeasureTime(ref _perfTracker.Trace))
                        ExecuteMessage(message);

                    _perfTracker.Tick();
                }
            }
        }
        catch(Exception ex)
        { 
            Brio.Log.Fatal("Eror in Mediator Pump: {ex}", ex); 
        }
    }

    public void Publish<T>(T message) where T : MessageBase
    {
        if(message.KeepThreadContext)
        {
            ExecuteMessage(message);
        }
        else
        {
            _queue.Writer.TryWrite(message);
        }
    }

    public void Subscribe<T>(IMediatorSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        var subContainer = _subscribers.GetOrAdd(typeof(T), static _ => new SubscriberContainer());

        lock(_addRemoveLock)
        {
            var subAction = new SubscriberAction(subscriber, action);
            if(subContainer.Subscribers.Any(s => s.Subscriber == subscriber))
                return;

            subContainer.SubAction ??= BuildAction<T>();

            subContainer.Subscribers = Resort(subContainer.Subscribers.Add(subAction));
        }
    }

    public void Unsubscribe<T>(IMediatorSubscriber subscriber) where T : MessageBase
    {
        if(!_subscribers.TryGetValue(typeof(T), out var subContainer))
            return;

        lock(_addRemoveLock)
        {
            var removed = subContainer.Subscribers.Where(s => s.Subscriber == subscriber);
            if(!removed.Any()) 
                return;

            subContainer.Subscribers = Resort(subContainer.Subscribers.RemoveAll(s => s.Subscriber == subscriber));
            foreach(var r in removed) 
                _lastErrorTime.TryRemove(r, out _);
        }
    }

    internal void UnsubscribeAll(IMediatorSubscriber subscriber)
    {
        lock(_addRemoveLock)
        {
            foreach(var (type, subContainer) in _subscribers)
            {
                var removed = subContainer.Subscribers.Where(s => s.Subscriber == subscriber);
                if(!removed.Any())
                    continue;

                subContainer.Subscribers = Resort(subContainer.Subscribers.RemoveAll(s => s.Subscriber == subscriber));
                foreach(var r in removed) 
                    _lastErrorTime.TryRemove(r, out _);

                Brio.Log.Debug("{sub} unsubscribed from {msg}", subscriber.GetType().Name, type.Name);
            }
        }
    }

    private void ExecuteMessage(MessageBase message)
    {
        if(!_subscribers.TryGetValue(message.GetType(), out var subContainer))
            return;

        var subscribers = subContainer.Subscribers;
        if(subscribers.IsDefaultOrEmpty)
            return;

        subContainer.SubAction!(subscribers, message, this);
    }

    public void PrintSubscriberInfo()
    {
        Brio.Log.Info("=== Mediator Subscriber Info ===");

        var subs = _subscribers
            .SelectMany(kv => kv.Value.Subscribers.Select(s => s.Subscriber))
            .Distinct()
            .OrderBy(p => p.GetType().FullName, StringComparer.Ordinal);

        foreach(var subscriber in subs)
        {
            Brio.Log.Info("Subscriber {type}: {sub}", subscriber.GetType().Name, subscriber.ToString() ?? "Unknown");

            var sb = new StringBuilder("=> ");
            foreach(var kv in _subscribers)
                if(kv.Value.Subscribers.Any(s => s.Subscriber == subscriber))
                    sb.Append(kv.Key.Name).Append(", ");

            if(sb.Length > 3)
                Brio.Log.Info("{sb}", sb.ToString().TrimEnd(' ', ','));

            Brio.Log.Info("---");
        }
    }

    private static Action<ImmutableArray<SubscriberAction>, MessageBase, Mediator> BuildAction<T>() where T : MessageBase
    {
        return static (subscribers, message, self) =>
        {
            foreach(var sub in subscribers)
            {
                try
                {
                    ((Action<T>)sub.Action).Invoke((T)message);
                }
                catch(Exception ex)
                {
                    if(self._lastErrorTime.TryGetValue(sub, out var last) && last.AddSeconds(10) > DateTime.UtcNow)
                        continue;

                    Brio.Log.Error("Error executing {type} for subscriber {subscriber}: {ex}", typeof(T).Name, sub.Subscriber.GetType().Name, ex.InnerException?.Message ?? ex.Message);

                    self._lastErrorTime[sub] = DateTime.UtcNow;
                }
            }
        };
    }

    private static ImmutableArray<SubscriberAction> Resort(ImmutableArray<SubscriberAction> source)
    {
        var builder = ImmutableArray.CreateBuilder<SubscriberAction>(source.Length);
       
        foreach(var sub in source)
            if(sub.Subscriber is IHighPriorityMediatorSubscriber)
                builder.Add(sub);
       
        foreach(var sub in source)
            if(sub.Subscriber is not IHighPriorityMediatorSubscriber) 
                builder.Add(sub);
     
        return builder.MoveToImmutable();
    }

    private class SubscriberContainer
    {
        public ImmutableArray<SubscriberAction> Subscribers = [];
        public Action<ImmutableArray<SubscriberAction>, MessageBase, Mediator>? SubAction;
    }

    private sealed class SubscriberAction(IMediatorSubscriber subscriber, object action)
    {
        public object Action { get; } = action;
        public IMediatorSubscriber Subscriber { get; } = subscriber;

        public override bool Equals(object? obj)
            => obj is SubscriberAction o && Subscriber == o.Subscriber && Action == o.Action;

        public override int GetHashCode()
            => HashCode.Combine(Subscriber, Action);
    }

    public void Dispose()
    {
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        _loopCTS.Dispose();

        GC.SuppressFinalize(this);
    }
}

public interface IHighPriorityMediatorSubscriber : IMediatorSubscriber { }

public interface IMediatorSubscriber : IDisposable
{
    Mediator Mediator { get; }
}

public abstract class MediatorSubscriberBase(Mediator mediator) : IMediatorSubscriber
{
    public Mediator Mediator { get; } = mediator;

    public virtual void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        GC.SuppressFinalize(this);
    }
}

public abstract record MessageBase
{
    public virtual bool KeepThreadContext => false;
}

public record SameThreadMessage : MessageBase
{
    public override bool KeepThreadContext => true;
}
