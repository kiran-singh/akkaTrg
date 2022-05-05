// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using WinTail;

// make an actor system 
var myActorSystem = ActorSystem.Create("MyActorSystem");

Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
var consoleWriterActor = myActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

Props tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
IActorRef tailCoordinatorActor = myActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

Props fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoordinatorActor));
IActorRef fileValidatorActor = myActorSystem.ActorOf(fileValidatorActorProps, "validationActor");


// // // make our first actors!
// // IActorRef consoleWriterActor = myActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()),
// //     "consoleWriterActor");
// // IActorRef consoleReaderActor =
// //     myActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(consoleWriterActor)),
// //         "consoleReaderActor");
//             
// Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
// var consoleWriterActor = myActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");
//             
// Props validationActorProps = Props.Create(() => new ValidationActor(consoleWriterActor));
//
// IActorRef validationActor = myActorSystem.ActorOf(validationActorProps, "validationActor");
//

Props consoleReaderProps = Props.Create<ConsoleReaderActor>(fileValidatorActor);
IActorRef consoleReaderActor = myActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");
            
// tell console reader to begin
consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);
            
// blocks the main thread from exiting until the actor system is shut down
myActorSystem.WhenTerminated.Wait();