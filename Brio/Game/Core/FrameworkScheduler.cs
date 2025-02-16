﻿using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.Game.Core;

public class FrameworkScheduler(IFramework framework, bool alwaysDefer) : TaskScheduler
{
    private readonly IFramework _framework = framework;
    private readonly bool _alwaysDefer = alwaysDefer;

    private readonly LinkedList<Task> _tasks = new();

    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        lock(_tasks)
            return [.. _tasks];
    }

    protected override void QueueTask(Task task)
    {
        if(TryExecuteTaskInline(task, false))
            return;

        lock(_tasks)
            _tasks.AddLast(task);

        _framework.RunOnTick(task.RunSynchronously).ContinueWith((_) => TryDequeue(task));
    }

    protected sealed override bool TryDequeue(Task task)
    {
        lock(_tasks)
            return _tasks.Remove(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if(!_alwaysDefer && _framework.IsInFrameworkUpdateThread)
            return TryExecuteTask(task);

        return false;
    }
}
