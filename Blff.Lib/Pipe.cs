using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace blff
{
    public class Pipe
    {
        public const string PipeDefault = "blffqcl";
        private readonly string _name;
        private readonly int _timeout = 10;

        public Pipe(string name)
        {
            _name = name;
        }

        public Thread NewTransmissionThread(string message)
        {
            return new Thread(() => SendMessage(message));
        }

        private void SendMessage(string message)
        {
            using (var pipe = new NamedPipeClientStream(_name))
            {
                try
                {
                    pipe.Connect(_timeout * 1000);
                    pipe.ReadMode = PipeTransmissionMode.Message;
                    var msg = Encoding.UTF8.GetBytes(message);
                    pipe.Write(msg, 0, msg.Length);
                }
                catch (Exception ex)
                {
                    Util.Message(Util.Tag("pipe"), "error",
                        ex is TimeoutException ? $"Could not connect to pipe within {_timeout} seconds!" : ex.Message);

                    Util.LogError(ex.Message);
                }
            }
        }
    }
}