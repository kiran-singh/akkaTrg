using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors;

public class PerformanceCounterCoordinatorActor : ReceiveActor
{
    public class Watch
    {
        public Metric.CounterType Counter { get; }

        public Watch(Metric.CounterType counter)
        {
            Counter = counter;
        }
    }

    public class Unwatch
    {
        public Metric.CounterType Counter { get; }

        public Unwatch(Metric.CounterType counter)
        {
            Counter = counter;
        }
    }

    private static readonly Dictionary<Metric.CounterType, Func<PerformanceCounter>> CounterGenerators = new()
    {
        { Metric.CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true) },
        { Metric.CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true) },
        { Metric.CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true) },
    };

    private static readonly Dictionary<Metric.CounterType, Func<Series>> CounterSeries = new()
    {
        {
            Metric.CounterType.Cpu,
            () => new Series(Metric.CounterType.Cpu.ToString())
                { ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen }
        },
        {
            Metric.CounterType.Memory,
            () => new Series(Metric.CounterType.Memory.ToString())
                { ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue }
        },
        {
            Metric.CounterType.Disk,
            () => new Series(Metric.CounterType.Disk.ToString())
                { ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed }
        },
    };

    private readonly Dictionary<Metric.CounterType, IActorRef> _counterActors;

    private readonly IActorRef _chartingActor;

    public PerformanceCounterCoordinatorActor(IActorRef chartingActor) :
        this(chartingActor, new Dictionary<Metric.CounterType, IActorRef>())
    {
    }

    public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
        Dictionary<Metric.CounterType, IActorRef> counterActors)
    {
        _chartingActor = chartingActor;
        _counterActors = counterActors;

        Receive<Watch>(watch =>
        {
            if (!_counterActors.ContainsKey(watch.Counter))
            {
                // create a child actor to monitor this counter if
                // one doesn't exist already
                var counterActor = Context.ActorOf(Props.Create(() =>
                    new PerformanceCounterActor(watch.Counter.ToString(),
                        CounterGenerators[watch.Counter])));

                // add this counter actor to our index
                _counterActors[watch.Counter] = counterActor;
            }

            // register this series with the ChartingActor
            _chartingActor.Tell(new ChartingActor.AddSeries(
                CounterSeries[watch.Counter]()));

            // tell the counter actor to begin publishing its
            // statistics to the _chartingActor
            _counterActors[watch.Counter].Tell(new Metric.SubscribeCounter(watch.Counter,
                _chartingActor));
        });

        Receive<Unwatch>(unwatch =>
        {
            if (!_counterActors.ContainsKey(unwatch.Counter))
            {
                return; // noop
            }

            // unsubscribe the ChartingActor from receiving any more updates
            _counterActors[unwatch.Counter].Tell(new Metric.UnsubscribeCounter(unwatch.Counter, _chartingActor));

            // remove this series from the ChartingActor
            _chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
        });
    }
}