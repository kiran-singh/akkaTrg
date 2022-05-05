using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors;

public class PerformanceCounterActor : ReceiveActor
{
    private readonly string _seriesName;
    private readonly Func<PerformanceCounter> _performanceCounterGenerator;
    private PerformanceCounter _counter;

    private readonly HashSet<IActorRef> _subscriptions;
    private readonly ICancelable _cancelPublishing;

    public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
    {
        _seriesName = seriesName;
        _performanceCounterGenerator = performanceCounterGenerator;
        _subscriptions = new HashSet<IActorRef>();
        _cancelPublishing = new Cancelable(Context.System.Scheduler);

        Receive<GatherMetrics>(_ =>
        {
            var metric = new Metric(_seriesName, _counter.NextValue());
            foreach (var subscription in _subscriptions)
            {
                subscription.Tell(metric);
            }
        });
        
        Receive<Metric.SubscribeCounter>(subscribeCounter =>
            _subscriptions.Add(subscribeCounter.Subscriber));
        
        Receive<Metric.UnsubscribeCounter>(unsubscribeCounter =>
            _subscriptions.Remove(unsubscribeCounter.Subscriber));
    }
    
    protected override void PreStart()
    {
        _counter = _performanceCounterGenerator();
        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromMilliseconds(250), 
            TimeSpan.FromMilliseconds(250), 
            Self,
            new GatherMetrics(),
            Self,
            _cancelPublishing);
    }

    protected override void PostStop()
    {
        try
        {
            _cancelPublishing.Cancel(false);
            _counter.Dispose();
        }
        catch
        {
        }
        finally
        {
            base.PostStop();
        }
    }
}