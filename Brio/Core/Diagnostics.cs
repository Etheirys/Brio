using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Brio.Core;

public static class Diagnostics
{
    /// <summary>
    /// Begins a timed scope that records into <paramref name="trace"/> when disposed.
    /// Use with a using declaration.
    /// </summary>
    public static DiagnosticScope MeasureTime(ref DiagnosticTrace trace, long customTraceData = 0, bool logOnDispose = false, string logLabel = "Trace")
        => new(ref trace, customTraceData, logOnDispose, logLabel);

    /// <summary>
    /// Begins a timed scope that records into <paramref name="trace"/> when disposed.
    /// Use with a using declaration.
    /// </summary>
    public static DiagnosticScope MeasureTime(ref DiagnosticTrace trace, bool logOnDispose)
        => new(ref trace, 0, logOnDispose);

    /// <summary>
    /// Begins a timed scope that records into <paramref name="trace"/> when disposed.
    /// Use with a using declaration.
    /// </summary>
    public static DiagnosticScope MeasureTime(ref DiagnosticTrace trace, bool logOnDispose, string logLabel)
        => new(ref trace, 0, logOnDispose, logLabel);
  
    public static bool TickSlowFrame(double ms, double thresholdMs, ref int cooldown, int cooldownFrames)
    {
        if(ms > thresholdMs && cooldown <= 0)
        {
            cooldown = cooldownFrames;
            return true;
        }

        if(cooldown > 0)
            cooldown--;

        return false;
    }

    public static bool TickFrameCounter(ref int counter, int interval)
    {
        if(++counter >= interval)
        {
            counter = 0;
            return true;
        }

        return false;
    }
}

public ref struct DiagnosticScope : IDisposable
{
    private readonly long _start;
    private ref DiagnosticTrace _trace;
    private readonly double _customData;
    private readonly bool _logOnDispose;
    private readonly string _logLabel;

    internal DiagnosticScope(ref DiagnosticTrace trace, double customData, bool logOnDispose = false, string logLabel = "Trace")
    {
        _start = Stopwatch.GetTimestamp();
        _trace = ref trace;
        _customData = customData;
        _logOnDispose = logOnDispose;
        _logLabel = logLabel;
    }

    public void Dispose()
    {
        _trace.Record(Stopwatch.GetTimestamp() - _start, _customData);

        if(_logOnDispose)
        {
            _trace.Log(_logLabel);
        }
    }

    public void Record()
    {
        _trace.Record(Stopwatch.GetTimestamp() - _start, _customData);
    }
}

public struct DiagnosticTrace()
{
    public int SampleCount;

    public long TotalTicks;
    public long LastTicks;
    public long MinTicks;
    public long MaxTicks;

    public double CustomData;

    public void Record(long ticks, double customData = 0)
    {
        LastTicks = ticks;
        TotalTicks += ticks;

        if(SampleCount == 0 || ticks < MinTicks)
            MinTicks = ticks;

        if(ticks > MaxTicks)
            MaxTicks = ticks;

        SampleCount++;
        CustomData += customData;
    }

    public readonly void Log(string label = "Trace")
        => Brio.Log.Verbose($"[Diagnostics]:[{label}] Samples:[{SampleCount}] | avg={AvgMs:F3}ms min={MinMs:F3}ms max={MaxMs:F3}ms | avgdata={AvgCustomData:F1}");

    public void Reset()
        => this = default;

    public readonly double LastMs => TicksToMs(LastTicks);
    public readonly double AvgMs => SampleCount > 0 ? TicksToMs(TotalTicks / SampleCount) : 0;
    public readonly double MinMs => SampleCount > 0 ? TicksToMs(MinTicks) : 0;
    public readonly double MaxMs => TicksToMs(MaxTicks);
    public readonly double AvgCustomData => SampleCount > 0 ? CustomData / SampleCount : 0;

    private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
}

/// <summary>
/// Tracks a frame counter and a slow frame counter cooldown against thresholds.
/// Logs can be triggered when the frame counter reaches the log interval or when a slow frame is detected and not on a cooldown.
/// </summary>
public struct DiagnosticTracker(string tag, int logInterval, double slowFrameThresholdMs = 100, int slowFrameCooldownFrames = 5000, Dictionary<string, DiagnosticTrace>? diagnosticTraces = null)
{
    private readonly Dictionary<string, DiagnosticTrace>? _dynamicTraces = diagnosticTraces;

    private int _frameCounter;
    private int _slowFrameCooldown;

    public readonly string Tag = tag;
    public readonly int LogInterval = logInterval;
    public readonly double SlowFrameThresholdMs = slowFrameThresholdMs;
    public readonly int SlowFrameCooldownFrames = slowFrameCooldownFrames;

    public DiagnosticTrace Trace;

    /// <summary>
    /// Registers a new named trace.
    /// </summary>
    public readonly void AddTrace(string label, ref DiagnosticTrace trace)
        => _dynamicTraces?.Add(label, trace);

    /// <summary>
    /// Removes a named trace.
    /// </summary>
    public readonly void RemoveTrace(string label)
        => _dynamicTraces?.Remove(label);

    /// <summary>
    /// Returns a <see langword="ref"/> to the sub-trace at <paramref name="index"/> for use with <see cref="Diagnostics.MeasureTime"/>.
    /// </summary>
    public readonly ref DiagnosticTrace GetTrace(string index)
        => ref CollectionsMarshal.GetValueRefOrNullRef(_dynamicTraces!, index);

    /// <summary>
    /// Checks if the given frame time exceeds the slow frame threshold and returns <c>true</c> when a slow frame is detected.
    /// </summary>
    public bool OnSlowFrame(double ms) // TODO FIX: There is a bug here where if you want to run this with more then one Trace in a row the FrameCooldown will have been hit so it won't fire
        => Diagnostics.TickSlowFrame(ms, SlowFrameThresholdMs, ref _slowFrameCooldown, SlowFrameCooldownFrames);

    /// <summary>
    /// Increments the frame counter. When the log interval is reached, logs and resets <see cref="Trace"/>
    /// and all registered sub-traces, then returns <c>true</c>.
    /// </summary>
    public bool Tick(bool doLogging = true)
    {
        if(!Diagnostics.TickFrameCounter(ref _frameCounter, LogInterval))
            return false;

        if(doLogging)
        {
            Log();
        }

        return true;
    }

    /// <summary>
    /// Outputs the trace's to Brio.Log, then resets them.
    /// </summary>
    public void Log()
    {
        Trace.Log(Tag);
        Trace.Reset();

        if(_dynamicTraces == null)
            return;

        double totalAverage = 0; 
        foreach(var tracesKVP in _dynamicTraces)
        {
            var trace = tracesKVP.Value;
            Brio.Log.Verbose($"  [Diagnostics]:[{tracesKVP.Key}] avg={trace.AvgMs:F3}ms max={trace.MaxMs:F3}ms");
            totalAverage += trace.AvgMs;
            trace.Reset();
        }
        Brio.Log.Verbose($"  -----------------------------------------------------");
        Brio.Log.Verbose($"  [Diagnostics]:[Total Average] avg={totalAverage:F3}ms");
        Brio.Log.Verbose($"  -----------------------------------------------------");

    }
}

