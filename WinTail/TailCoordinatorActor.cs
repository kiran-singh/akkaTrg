using Akka.Actor;

namespace WinTail;

public class TailCoordinatorActor : UntypedActor
{
    public class StartTail
    {
        public string FilePath { get; }
        public IActorRef ReporterActor { get; }

        public StartTail(string filePath, IActorRef reporterActor)
        {
            FilePath = filePath;
            ReporterActor = reporterActor;
        }
    }
    
    public class StopTail
    {
        public string FilePath { get; }

        public StopTail(string filePath)
        {
            FilePath = filePath;
        }
    }
    
    protected override void OnReceive(object message)
    {
        if (message is StartTail startTail)
        {
            Context.ActorOf(Props.Create(
                () => new TailActor(startTail.ReporterActor, startTail.FilePath)));
        }
    }
    
    protected override SupervisorStrategy SupervisorStrategy() =>
        new OneForOneStrategy (
            maxNrOfRetries: 10, 
            withinTimeRange: TimeSpan.FromSeconds(30), 
            localOnlyDecider: exception =>
            {
                return exception switch
                {
                    ArithmeticException => Directive.Resume,
                    NotSupportedException => Directive.Stop,
                    _ => Directive.Restart
                };
            });
}