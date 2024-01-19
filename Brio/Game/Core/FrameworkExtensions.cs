using Dalamud.Plugin.Services;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Brio.Game.Core;

internal static class FrameworkExtensions
{
    public static Task RunUntilSatisfied(
        this IFramework framework,
        Func<bool> condition,
        Action<bool> onSatisfied,
        int attempts,
        int dontStartFor = 0,
        bool deferOnceOnSuccess = false,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = ""
    )
    {
        return RunUntilSatisfied(framework, condition, (v) =>
        {
            onSatisfied.Invoke(v);
            return true;
        }, attempts, dontStartFor, deferOnceOnSuccess, callerFile, callerLine, callerMember);
    }

    public static Task<T> RunUntilSatisfied<T>(
        this IFramework framework,
        Func<bool> condition,
        Func<bool, T> onSatisfied,
        int attempts,
        int dontStartFor = 0,
        bool deferOnceOnSuccess = false,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = ""
    )
    {
        var callbackTask = new TaskCompletionSource<T>();

        var newTask = new DeferredTask<T>()
        {
            CallbackTask = callbackTask,
            ConditionAction = () => condition.Invoke(),
            CompleteAction = onSatisfied.Invoke,
            MaxFrames = attempts,
            DeferOnceMore = deferOnceOnSuccess,
            DebugPath = $"{callerFile}:{callerLine} - {callerMember}"
        };

        framework.RunOnTick(() =>
        {
            ProcessTask(framework, newTask);
        },
        delayTicks: dontStartFor);

        return callbackTask.Task;
    }

    private static void ProcessTask<T>(IFramework framework, DeferredTask<T> task)
    {
        var thisTick = task.TickCount++;
        bool result = false;

        try
        {
            result = task.ConditionAction();
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, $"Exception running condition action. {task}");
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
                return;
            }
        }
        else
        {
            if(thisTick >= task.MaxFrames)
            {
                Brio.Log.Warning($"Task timed out. {task}");
                CompleteTask(task, false, null);
            }
            else
            {
                framework.RunOnTick(() => ProcessTask(framework, task));
            }
        }
    }

    private static void CompleteTask<T>(DeferredTask<T> task, bool success, Exception? e)
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
                var result = task.CompleteAction.Invoke(success);
                task.CallbackTask.SetResult(result);
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, $"Exception running completion action. {task}");
            task.CallbackTask.SetException(ex);
        }
    }
}

class DeferredTask<T>
{
    public TaskCompletionSource<T> CallbackTask { get; set; } = null!;
    public Func<bool> ConditionAction { get; set; } = null!;
    public Func<bool, T> CompleteAction { get; set; } = null!;
    public string DebugPath { get; set; } = null!;
    public int MaxFrames { get; set; }
    public int TickCount { get; set; } = -1;
    public bool DeferOnceMore { get; set; } = false;

    public override string ToString() => $"{DebugPath}. T: {MaxFrames} C: {TickCount}";
}
