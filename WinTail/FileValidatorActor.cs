using Akka.Actor;

namespace WinTail;

public class FileValidatorActor : UntypedActor
{
    private readonly IActorRef _consoleWriterActor;

    public FileValidatorActor(IActorRef consoleWriterActor) => _consoleWriterActor = consoleWriterActor;

    protected override void OnReceive(object message)
    {
        var msg = message as string;
        if (string.IsNullOrEmpty(msg))
        {
            _consoleWriterActor.Tell(new Messages.NullInputError("Input was blank. Please try again.\n"));
            Sender.Tell(new Messages.ContinueProcessing());
        }
        else
        {
            if (File.Exists(msg))
            {
                _consoleWriterActor.Tell(new Messages.InputSuccess($"Starting processing for {msg}"));
                Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor")
                    .Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
            }
            else
            {
                _consoleWriterActor.Tell(new Messages.ValidationError($"{msg} is not an existing URI on disk."));
                Sender.Tell(new Messages.ContinueProcessing());
            }
        }
    }
}