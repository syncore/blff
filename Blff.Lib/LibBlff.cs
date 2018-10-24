using System;
using System.IO;
using System.Linq;

namespace blff
{
    // blff: bnlauncher (hardware) fingerprint finder, syncore <syncore@syncore.org> 2018.
    public class LibBlff
    {
        public const string Version = "0.1";
        public const string ErrorLog = "blff_error.log";

        private const string FDelay = "-d=";
        private const string FRetry = "-r=";
        private const string FPipe = "-p=";
        private const string FWriteFile = "-f=";
        private const string FTest = "-t";
        private const string FLogError = "-e";

        private static int _delay = Finder.DelayDefault;
        private static int _retries = Finder.RetryDefault;
        private static string _pipeName = Pipe.PipeDefault;
        private static string _outFilename = "fp.json";
        private static bool _usePipe;
        private static bool _writeFile;
        private static bool _isTestMode;

        public void Run(string[] args)
        {
            ParseArguments(args);

            var fp = _isTestMode ? "TEST12345TEST12345TEST12345TEST12345TEST" : new Finder(_delay, _retries).Find();
            var fpj = string.IsNullOrWhiteSpace(fp) ? "{\"fp\": null}" : "{\"fp\":\"" + fp + "\"}";

            if (!string.IsNullOrWhiteSpace(fp))
                Util.Message(string.Empty, $"{(_isTestMode ? "success (test mode)" : "success")}", $"\t FP is: {fp}");

            if (_writeFile)
            {
                Util.Message(Util.Tag("file"), string.Empty, $"Writing result to: {_outFilename}");
                Util.WriteFile(fpj, _outFilename);
            }
                
            if (_usePipe)
            {
                var pipe = new Pipe(_pipeName);
                var tt = pipe.NewTransmissionThread(fpj);
                tt.Start();
                tt.Join();
            }
        }

        private void ParseArguments(string[] args)
        {
            if (args.Any(a => a.Contains(FDelay)))
                _delay = ParseIntFlag(args.FirstOrDefault(a => a.Contains(FDelay.ToLowerInvariant())), FDelay, Finder.DelayDefault, Finder.DelayMin);

            if (args.Any(a => a.Contains(FRetry)))
                _retries = ParseIntFlag(args.FirstOrDefault(a => a.Contains(FRetry.ToLowerInvariant())), FRetry, Finder.RetryDefault, Finder.RetryDefault);

            if (args.Any(a => a.Contains(FPipe)))
            {
                _pipeName = ParseStrFlag(args.FirstOrDefault(a => a.Contains(FPipe.ToLowerInvariant())), FPipe, Pipe.PipeDefault);
                _usePipe = true;
            }

            if (args.Any(a => a.Contains(FWriteFile)))
            {
                _outFilename = string.Join("_", ParseStrFlag(args.FirstOrDefault(a => a.Contains(FWriteFile.ToLowerInvariant())),
                    FWriteFile, _outFilename).Split(Path.GetInvalidFileNameChars()));
                _writeFile = true;
            }

            if (args.Any(a => a.Contains(FTest.ToLowerInvariant())))
                _isTestMode = true;

            if (args.Any(a => a.Contains(FLogError.ToLowerInvariant())))
                Util.LogErrors = true;
        }

        private static int ParseIntFlag(string val, string flag, int defaultVal, int minVal)
        {
            if (int.TryParse(val.Replace(flag, string.Empty).Replace("\"", string.Empty).Trim(), out var parsed) && parsed >= minVal)
                return parsed;

            Console.WriteLine($"WARN\t Invalid {flag} value: {(!string.IsNullOrWhiteSpace(val) ? val.Replace(flag, string.Empty) : val)}" +
                $" Must be > {minVal}. Using default of {defaultVal} instead.");

            return defaultVal;
        }

        private string ParseStrFlag(string val, string flag, string defaultVal)
        {
            var parsed = val.Replace(flag, string.Empty).Replace("\"", string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(parsed))
                return parsed;

            Console.WriteLine($"WARN\t Invalid {flag} value: {parsed}. Cannot be empty. Using default of {defaultVal} instead.");

            return defaultVal;
        }
    }
}
