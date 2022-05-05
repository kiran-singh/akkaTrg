using Akka.Actor;

namespace ChartApp.Actors;

public class GatherMetrics { }

public class Metric
{
    public string Series { get; }
    public float CounterValue { get; }

    public Metric(string series, float counterValue)
    {
        Series = series;
        CounterValue = counterValue;
    }
    
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    public class SubscribeCounter
    {
        public CounterType Counter { get; }
        public IActorRef Subscriber { get; }

        public SubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Counter = counter;
            Subscriber = subscriber;
        }
    }

    public class UnsubscribeCounter
    {
        public CounterType Counter { get; }
        public IActorRef Subscriber { get; }

        public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Counter = counter;
            Subscriber = subscriber;
        }
    }
}