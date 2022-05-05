using System.Text;
using Akka.Actor;

namespace WinTail;

public class TailActor : UntypedActor
{
    public class FileWrite
    {
        public FileWrite(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; private set; }
    }

    public class FileError
    {
        public FileError(string fileName, string reason)
        {
            FileName = fileName;
            Reason = reason;
        }

        public string FileName { get; private set; }

        public string Reason { get; private set; }
    }
    
    public class InitialRead
    {
        public InitialRead(string fileName, string text)
        {
            FileName = fileName;
            Text = text;
        }

        public string FileName { get; }
        public string Text { get; }
    }

    private readonly IActorRef _reporterActor;
    private readonly StreamReader _fileStreamReader;

    public TailActor(IActorRef reporterActor, string filePath)
    {
        _reporterActor = reporterActor;

        var observer = new FileObserver(Self, Path.GetFullPath(filePath));
        observer.Start();

        Stream fileStream = new FileStream(Path.GetFullPath(filePath),
            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

        var text = _fileStreamReader.ReadToEnd();
        Self.Tell(new InitialRead(filePath, text));
    }
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case FileWrite:
            {
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
                break;
            }
            case FileError fileError:
                _reporterActor.Tell($"Tail error: {fileError.Reason}");
                break;
            case InitialRead initialRead:
                _reporterActor.Tell(initialRead.Text);
                break;
        }
    }
}