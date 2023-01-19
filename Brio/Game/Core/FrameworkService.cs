using Brio.Core;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Brio.Game.Core;

public class FrameworkService : ServiceBase<FrameworkService>
{
    private List<DeferredTask> _deferredTasks = new();

    public Task<bool> RunUntilSatisfied(
        Func<bool> condition,
        Action<bool> onSatisfied,
        int attempts,
        int dontStartFor = 0,
        bool waitOneMore = false,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = ""
    )
    {
        var callbackTask = new TaskCompletionSource<bool>();

        var newTask = new DeferredTask()
        {
            CallbackTask= callbackTask,
            ConditionAction = () => condition.Invoke(),
            CompleteAction = onSatisfied.Invoke,
            MaxFrames = attempts,
            StartFrame = dontStartFor,
            DeferOnceMore = waitOneMore,
            DebugPath = $"{callerFile}:{callerLine} - {callerMember}"
        };

        _deferredTasks.Add(newTask);

        return callbackTask.Task;
    }

    public override void Tick()
    {
        lock(_deferredTasks)
        {
            for(int i = 0; i < _deferredTasks.Count; i++)
            {
                var task = _deferredTasks[i];
                task.TickCount++;

                if(task.TickCount >= task.StartFrame)
                {
                    var conditionSatisfied = CheckTask(task);

                    if(conditionSatisfied.success)
                    {
                        if(task.DeferOnceMore)
                        {
                            task.DeferOnceMore = false;
                            task.ConditionAction = () => true;
                        }
                        else
                        {
                            _deferredTasks.RemoveAt(i--);
                            CompleteTask(task, true, null);
                        }
                    }
                    else if(conditionSatisfied.error != null || task.MaxFrames <= task.TickCount)
                    {
                        _deferredTasks.RemoveAt(i--);

                        if(task.MaxFrames <= task.TickCount)
                        {
                            PluginLog.Warning($"Task timed out. {task}");
                            CompleteTask(task, false, null);
                        }
                        else
                        {
                            CompleteTask(task, false, conditionSatisfied.error);
                        }
                    }

                }
            }
        }
    }

    private (bool success, Exception? error) CheckTask(DeferredTask task)
    {
        try
        {
            return (task.ConditionAction(), null);
        }
        catch(Exception ex)
        {
            PluginLog.Warning(ex, $"Exception running condition action. {task}");
            return (false, ex);
        }
    }

    private void CompleteTask(DeferredTask task, bool success, Exception? e)
    {
        try
        {
            if(e != null)
            {
                task.CompleteAction.Invoke(false);
                task.CallbackTask.SetException(e);
            }
            else
            {
                task.CompleteAction.Invoke(success);
                task.CallbackTask.SetResult(success);
            }
        }
        catch(Exception ex)
        {
            PluginLog.Warning(ex, $"Exception running completion action. {task}");
            task.CallbackTask.SetResult(true);
        }
    }

    public override void Dispose()
    {
        lock(_deferredTasks)
        {
            _deferredTasks.Clear();
        }
    }
}

class DeferredTask
{
    public TaskCompletionSource<bool> CallbackTask { get; set; } = null!;
    public Func<bool> ConditionAction { get; set; } = null!;
    public Action<bool> CompleteAction { get; set; } = null!;
    public string DebugPath { get; set; } = null!;
    public int MaxFrames { get; set; }
    public int StartFrame { get; set; }
    public int TickCount { get; set; } = -1;
    public bool DeferOnceMore { get; set; } = false;

    public override string ToString() => $"{DebugPath}. T: {MaxFrames} C: {TickCount}";
}
