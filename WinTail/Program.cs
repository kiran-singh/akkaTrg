// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using WinTail;

// make an actor system 
var myActorSystem = ActorSystem.Create("MyActorSystem");

Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
var consoleWriterActor = myActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

Props tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
myActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

Props fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor));
myActorSystem.ActorOf(fileValidatorActorProps, "validationActor");

Props consoleReaderProps = Props.Create<ConsoleReaderActor>();
IActorRef consoleReaderActor = myActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");
            
// tell console reader to begin
consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);
            
// blocks the main thread from exiting until the actor system is shut down
myActorSystem.WhenTerminated.Wait();