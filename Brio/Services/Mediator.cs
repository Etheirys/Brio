using Brio.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Brio.Services;

// This is based on Mare's Mediator; thanks Dark!
//

public class Mediator() : IDisposable
{
    private readonly Lock _addRemoveLock = new();

    private readonly CancellationTokenSource _loopCTS = new();
    private readonly ConcurrentDictionary<object, DateTime> _lastErrorTime = [];

    private readonly ConcurrentQueue<MessageBase> _messageQueue = new();
    private readonly ConcurrentDictionary<Type, MethodInfo?> _genericExecuteMethods = new();
    private readonly ConcurrentDictionary<Type, HashSet<SubscriberAction>> _subscribers = [];
   
    private DiagnosticTracker _perfTracker = new("Mediator", logInterval: 700, slowFrameThresholdMs: 5.0, slowFrameCooldownFrames: 8000,
        new Dictionary<string, DiagnosticTrace>
        {
                 //  "null", new DiagnosticTrace() },
        }
    ); 

    public void PrintSubscriberInfo()
    {
        Brio.Log.Info("=== Mediator Subscriber Info ===");
        foreach(var subscriber in _subscribers.SelectMany(c => c.Value.Select(v => v.Subscriber))
            .DistinctBy(p => p).OrderBy(p => p.GetType().FullName, StringComparer.Ordinal).ToList())
        {
            Brio.Log.Info("Subscriber {type}: {sub}", subscriber.GetType().Name, subscriber?.ToString() ?? "Unknown Subscriber");

            StringBuilder sb = new();
            sb.Append("=> ");

            var list = _subscribers.Where(item => item.Value.Any(v => v.Subscriber == subscriber)).ToList();
            for(int i = 0; i < list.Count; i++)
                sb.Append(list[i].Key.Name).Append(", ");

            if(!string.Equals(sb.ToString(), "=> ", StringComparison.Ordinal))
                Brio.Log.Info("{sb}", sb.ToString().TrimEnd(' ', ','));

            Brio.Log.Info("---");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Brio.Log.Info("Starting Mediator");

        _ = Task.Run(async () =>
        {
            while(!_loopCTS.Token.IsCancellationRequested)
            {
                await Task.Delay(100, _loopCTS.Token).ConfigureAwait(false);

                HashSet<MessageBase> processedMessages = [];
                while(_messageQueue.TryDequeue(out var message))
                {
                    if(processedMessages.Contains(message))
                        continue;

                    processedMessages.Add(message);

                    using(Diagnostics.MeasureTime(ref _perfTracker.Trace))
                        ExecuteMessage(message);

                    _perfTracker.Tick();
                }
            }
        }, cancellationToken);

        Brio.Log.Info("Started Mediator");

        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _messageQueue.Clear();

        if(!_loopCTS.IsCancellationRequested)
            _loopCTS.Cancel();
        
        _loopCTS.Dispose();
        return Task.CompletedTask;
    }

    public void Publish<T>(T message) where T : MessageBase
    {
        if(message.KeepThreadContext)
        {
            ExecuteMessage(message);
        }
        else
        {
            _messageQueue.Enqueue(message);
        }
    }

    public void Subscribe<T>(IMediatorSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        lock(_addRemoveLock)
        {
            _subscribers.TryAdd(typeof(T), []);

            if(!_subscribers[typeof(T)].Add(new(subscriber, action)))
            {
                throw new InvalidOperationException("Already subscribed");
            }
        }
    }
    public void Unsubscribe<T>(IMediatorSubscriber subscriber) where T : MessageBase
    {
        lock(_addRemoveLock)
        {
            if(_subscribers.ContainsKey(typeof(T)))
            {
                _subscribers[typeof(T)].RemoveWhere(p => p.Subscriber == subscriber);
            }
        }
    }
    internal void UnsubscribeAll(IMediatorSubscriber subscriber)
    {
        lock(_addRemoveLock)
        {
            foreach(Type kvp in _subscribers.Select(k => k.Key))
            {
                int unSubbed = _subscribers[kvp]?.RemoveWhere(p => p.Subscriber == subscriber) ?? 0;
                if(unSubbed > 0)
                {
                    Brio.Log.Debug("{sub} unsubscribed from {msg}", subscriber.GetType().Name, kvp.Name);
                }
            }
        }
    }

    private void ExecuteMessage(MessageBase message)
    {
        if(!_subscribers.TryGetValue(message.GetType(), out HashSet<SubscriberAction>? subscribers) || subscribers == null || subscribers.Count == 0) return;

        List<SubscriberAction> subscribersCopy = [];
        lock(_addRemoveLock)
        {
            subscribersCopy = subscribers?.Where(s => s.Subscriber != null).OrderBy(k => k.Subscriber is IHighPriorityMediatorSubscriber ? 0 : 1).ToList() ?? [];
        }

        var msgType = message.GetType();
        if(!_genericExecuteMethods.TryGetValue(msgType, out var methodInfo))
        {
            _genericExecuteMethods[msgType] = methodInfo = GetType()
                 .GetMethod(nameof(ExecuteReflected), BindingFlags.NonPublic | BindingFlags.Instance)?
                 .MakeGenericMethod(msgType);
        }

        methodInfo!.Invoke(this, [subscribersCopy, message]);
    }
    private void ExecuteReflected<T>(List<SubscriberAction> subscribers, T message) where T : MessageBase
    {
        foreach(SubscriberAction subscriber in subscribers)
        {
            try
            {
                ((Action<T>)subscriber.Action).Invoke(message);
            }
            catch(Exception ex)
            {
                if(_lastErrorTime.TryGetValue(subscriber, out var lastErrorTime) && lastErrorTime.Add(TimeSpan.FromSeconds(10)) > DateTime.UtcNow)
                    continue;

                Brio.Log.Error("Error executing {type} for subscriber {subscriber}: {ex}",
                    message.GetType().Name, subscriber.Subscriber.GetType().Name, ex.InnerException?.Message ?? ex.Message);

                _lastErrorTime[subscriber] = DateTime.UtcNow;
            }
        }
    }

    public void Dispose()
    {
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private sealed class SubscriberAction(IMediatorSubscriber subscriber, object action)
    {
        public object Action { get; } = action;
        public IMediatorSubscriber Subscriber { get; } = subscriber;

        public override bool Equals(object? obj)
            => obj is SubscriberAction other && Subscriber == other.Subscriber;

        public override int GetHashCode()
            => Subscriber.GetHashCode();
    }
}

//

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
