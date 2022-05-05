using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        public const string StartCommand = "start";

        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                DoPrintInstructions();
            }
            
            GetAndValidateInput();
        }
        
        private void DoPrintInstructions()
        {
            Console.WriteLine("Please provide the URI of a log file on disk.\n");
            // Console.WriteLine("Write whatever you want into the console!");
            // Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            // Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }

        /// <summary>
        /// Reads input from console, validates it, then signals appropriate response
        /// (continue processing, error, success, etc.).
        /// </summary>
        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message) &&
                String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // if user typed ExitCommand, shut down the entire actor
                // system (allows the process to exit)
                Context.System.Terminate();
                return;
            }

            // otherwise, just hand message off to validation actor
            Context.ActorSelection("akka://MyActorSystem/user/validationActor").Tell(message);
        }
        
        private static bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }
    }
}