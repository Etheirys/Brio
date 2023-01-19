using Dalamud.Game;
using Dalamud.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Brio.Game.Core;

public static class FrameworkExtensions
{
    public static Task<bool> RunUntilSatisfied(
        this Framework framework,
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
            CallbackTask = callbackTask,
            ConditionAction = () => condition.Invoke(),
            CompleteAction = onSatisfied.Invoke,
            MaxFrames = attempts,
            DeferOnceMore = waitOneMore,
            DebugPath = $"{callerFile}:{callerLine} - {callerMember}"
        };

        Dalamud.Framework.RunOnTick(() =>
        {
            ProcessTask(framework, newTask);
        },
        delayTicks: dontStartFor);

        return callbackTask.Task;

    }


    private static void ProcessTask(Framework framework, DeferredTask task)
    {
        var thisTick = task.TickCount++;
        bool result = false;

        try
        {
            result = task.ConditionAction();
        }
        catch(Exception ex)
        {
            PluginLog.Warning(ex, $"Exception running condition action. {task}");
            CompleteTask(task, false, ex);
            return;
        }

        if(result)
        {
            if(task.DeferOnceMore)
            {
                framework.RunOnTick(() => CompleteTask(task, true, null));
            }
            else
            {
                CompleteTask(task, true, null);
            }
        }
        else
        {
            Dalamud.Framework.RunOnTick(() => ProcessTask(framework, task));
        }

        if(thisTick >= task.MaxFrames)
        {
            PluginLog.Warning($"Task timed out. {task}");
            CompleteTask(task, false, null);
        }
    }
        
    private static void CompleteTask(DeferredTask task, bool success, Exception? e)
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
}

class DeferredTask
{
    public TaskCompletionSource<bool> CallbackTask { get; set; } = null!;
    public Func<bool> ConditionAction { get; set; } = null!;
    public Action<bool> CompleteAction { get; set; } = null!;
    public string DebugPath { get; set; } = null!;
    public int MaxFrames { get; set; }
    public int TickCount { get; set; } = -1;
    public bool DeferOnceMore { get; set; } = false;

    public override string ToString() => $"{DebugPath}. T: {MaxFrames} C: {TickCount}";
}
