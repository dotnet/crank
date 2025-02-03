using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Crank.Agent.MachineCounters.OS;
using Microsoft.Crank.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using OperatingSystem = Microsoft.Crank.Models.OperatingSystem;

namespace Microsoft.Crank.Agent.MachineCounters;

public class MachineCountersController : IDisposable
{
    private EventPipeSession _eventPipeSession;
    private readonly Job _job;
    private readonly List<IMachinePerformanceCounterEmitter> _machinePerfCounters = new();

    private MachineCountersController(EventPipeSession eventPipeSession, Job job)
    {
        _eventPipeSession = eventPipeSession;
        _job = job;
    }

    public static MachineCountersController Build(Job job)
    {
        var client = new DiagnosticsClient(Environment.ProcessId);
        var providers = new[]
        {
            new EventPipeProvider(MachineCountersEventSource.Log.Name, eventLevel: EventLevel.Informational, (long)EventKeywords.All)
        };

        var session = client.StartEventPipeSession(providers, requestRundown: false);
        return new MachineCountersController(session, job);
    }

    public MachineCountersController RegisterCounters()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        if (_job.OperatingSystem == OperatingSystem.Windows)
        {
            var windowsCpu = new WindowsMachinePerformanceCounterEmitter(
                    performanceCounter: new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true),
                    measurementName: GetMachineMeasurementName("cpu"));
            RegisterMachineCpuMeasurement(windowsCpu);

            _machinePerfCounters.Add(windowsCpu);
        }
        else if (_job.OperatingSystem is OperatingSystem.Linux or OperatingSystem.OSX)
        {
            var linuxCpu = new LinuxMachinePerformanceCounterEmitter(GetMachineMeasurementName("cpu"), "vmstat");
            RegisterMachineCpuMeasurement(linuxCpu);

            _machinePerfCounters.Add(linuxCpu);
        }
#pragma warning restore CA1416 // Validate platform compatibility

        foreach (var counter in _machinePerfCounters)
        {
            counter.Start();
            Log.Info($"Started {counter.MeasurementName} counter ({counter.CounterName}) emitter");
        }

        return this;
    }

    public Task _streamCountersTask;
    public Task RunStreamCountersTask() => (_streamCountersTask = Task.Run(Stream));
    public Task RunStopCountersTask(Task cancellationTask) => Task.Run(() => Stop(cancellationTask));

    private void Stream()
    {
        var source = new EventPipeEventSource(_eventPipeSession.EventStream);
        Log.Info("Machine-level eventPipe created");
        source.Dynamic.All += ProcessEventData;

        try
        {
            Log.Info($"Processing machine-level eventPipe source ({_job.Service}:{_job.Id})...");
            source.Process();
            Log.Info($"Machine-level eventPipe source stopped ({_job.Service}:{_job.Id})");
        }
        catch (Exception e) when (e is not ObjectDisposedException)
        {
            if (e.Message == "Read past end of stream.")
            {
                // Expected if the process has exited by itself
                // and the event pipe is till trying to read from it
                Log.Warning($"[WARNING] Machine-level eventPipe reading an exited process");
            }
            else
            {
                Log.Error(e, "[ERROR] machine-level eventPipe error on `source.Process()`");
            }
        }
    }

    private async void Stop(Task cancellationTask)
    {
        Log.Info($"Waiting for machine-level eventPipe session to stop ({_job.Service}:{_job.Id})...");
        if (_streamCountersTask is not null)
        {
            await Task.WhenAny(_streamCountersTask, cancellationTask);
        }
        Log.Info($"Stopping machine-level eventPipe session ({_job.Service}:{_job.Id})...");

        if (_streamCountersTask.IsCompleted)
        {
            Log.Info($"Reason: machine-level eventPipe source has ended");
        }
        if (cancellationTask.IsCompleted)
        {
            Log.Info($"Reason: machine-level counters are being stopped");
        }

        try
        {
            // It also interrupts the source.Process() blocking operation
            await _eventPipeSession.StopAsync(default);

            Log.Info($"Machine-level eventPipe session stopped ({_job.Service}:{_job.Id})");
        }
        catch (ServerNotAvailableException)
        {
            Log.Info($"Machine-level eventPipe session interrupted, application has already exited ({_job.Service}:{_job.Id})");
        }
        catch (Exception e)
        {
            Log.Info($"Machine-level eventPipe session failed stopping ({_job.Service}:{_job.Id}): {e}");
        }
        finally
        {
            Dispose();
        }

        try
        {
            if (_machinePerfCounters is not null)
            {
                foreach (var counterEmitter in _machinePerfCounters)
                {
                    counterEmitter?.Dispose();
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error stopping machine-level counters");
        }
    }

    private void ProcessEventData(TraceEvent eventData)
    {
        if (eventData.ProviderName != MachineCountersEventSource.Log.Name)
        {
            return;
        }

        var counterName = eventData.PayloadValue(0) as string;
        var counterValue = eventData.PayloadValue(1);
        var timestamp = eventData.TimeStamp.ToUniversalTime();

        var measurement = new Measurement
        {
            Name = counterName,
            Value = counterValue,
            Timestamp = timestamp
        };

        _job.Measurements.Enqueue(measurement);
    }

    void RegisterMachineCpuMeasurement(IMachinePerformanceCounterEmitter counterEmitter)
    {
        _job.Metadata.Enqueue(new MeasurementMetadata
        {
            Source = "Agent Machine",
            Name = counterEmitter.MeasurementName,
            Aggregate = Operation.Avg,
            Reduce = Operation.Avg,
            Format = "n0",
            LongDescription = $"Machine-level counter: '{counterEmitter.CounterName}'",
            ShortDescription = "[machine] CPU total time (%)"
        });
    }

    private static string GetMachineMeasurementName(string measurementName)
    {
        return "machine/" + measurementName;
    }

    public void Dispose()
    {
        _eventPipeSession?.Dispose();
        _eventPipeSession = null;
    }
}
