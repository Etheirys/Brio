using Dalamud.Game;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Brio.Game.Core;

public class FrameworkUtils : IDisposable
{
    private List<DeferredTask> _deferredTasks = new();

    public FrameworkUtils()
    {
        Dalamud.Framework.Update += Framework_Update;
    }

    private void Framework_Update(Framework framework)
    {
        TickTasks();
    }

    public void RunDeferred(
        Action<bool> action,
        int delay = 0,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = ""
    )
    {
        var newTask = new DeferredTask()
        {
            ConditionAction = (task) => task.TickCount >= task.StartFrame,
            CompleteAction = action,
            MaxFrames = delay,
            StartFrame = delay,
            DebugPath = $"{callerFile}:{callerLine} - {callerMember}"
        };

        _deferredTasks.Add(newTask);
    }

    public void RunUntilSatisfied(
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
        var newTask = new DeferredTask()
        {
            ConditionAction = (_) => condition.Invoke(),
            CompleteAction = onSatisfied.Invoke,
            MaxFrames = attempts,
            StartFrame = dontStartFor,
            DeferOnceMore = waitOneMore,
            DebugPath = $"{callerFile}:{callerLine} - {callerMember}"
        };

        _deferredTasks.Add(newTask);
    }

    public void Dispose()
    {
        Dalamud.Framework.Update -= Framework_Update;
        _deferredTasks.Clear();
    }

    private void TickTasks()
    {
        for(int i = 0; i < _deferredTasks.Count; i++)
        {
            var task = _deferredTasks[i];
            task.TickCount++;

            if (task.TickCount >= task.StartFrame)
            {
                var conditionSatisfied = CheckTask(task);

                if (conditionSatisfied == true)
                {
                    if (task.DeferOnceMore)
                    {
                        task.DeferOnceMore = false;
                        task.ConditionAction = (_) => true;
                    }
                    else
                    {
                        _deferredTasks.RemoveAt(i--);
                        CompleteTask(task, true);
                    }
                }
                else if (conditionSatisfied == null || task.MaxFrames <= task.TickCount)
                {
                    if (task.MaxFrames <= task.TickCount)
                        PluginLog.Warning($"Task timed out. {task}");

                    _deferredTasks.RemoveAt(i--);
                    CompleteTask(task, false);
                }
            }
        }
    }

    private bool? CheckTask(DeferredTask task)
    {
        try
        {
            return task.ConditionAction(task);
        }
        catch (Exception ex)
        {
            PluginLog.Warning(ex, $"Exception running condition action. {task}");
            return null;
        }
    }

    private void CompleteTask(DeferredTask task, bool success)
    {
        try
        {
            task.CompleteAction.Invoke(success);
        } catch(Exception ex)
        {
            PluginLog.Warning(ex, $"Exception running completion action. {task}");
        }
    }
}

class DeferredTask
{
    public Func<DeferredTask, bool> ConditionAction { get; set; } = null!;
    public Action<bool> CompleteAction { get; set; } = null!;
    public string DebugPath { get; set; } = null!;
    public int MaxFrames { get; set; }
    public int StartFrame { get; set; }
    public int TickCount { get; set; } = -1;
    public bool DeferOnceMore { get; set; } = false;

    public override string ToString() => $"{DebugPath}. T: {MaxFrames} C: {TickCount}";
}