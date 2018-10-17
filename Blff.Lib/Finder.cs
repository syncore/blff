using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Process.NET.Extensions;
using Process.NET.Memory;

namespace blff
{
    public class Finder
    {
        public const int DelayMin = 8;
        public const int DelayDefault = 10;
        public const int RetryDefault = 4;

        private const string Xsfp = "x-src-fp:";
        private Dictionary<System.Diagnostics.Process, bool> _pStatus;
        private int _attemptCount = 1;
        private readonly int _delaySecs;
        private readonly int _retryCount;


        public Finder(int delaySecs = DelayDefault, int retries = RetryDefault)
        {
            _delaySecs = delaySecs;
            _retryCount = retries;
        }

        public string Find()
        {
            var tag = Util.Tag("find");

            try // srps!
            {
                _pStatus = new Dictionary<System.Diagnostics.Process, bool>();

                // start
                Open();

                var proc = _pStatus.FirstOrDefault(p => !p.Value).Key;
                if (proc == null)
                {
                    Util.Message(tag, "error", "Did not get non-renderer process.");
                    return Retry();
                }

                Util.Message(tag, string.Empty, $"Starting with a delay of {_delaySecs} secs and making {_retryCount - _attemptCount} " +
                    $"more {(_retryCount - _attemptCount != 1 ? "attempts" : "attempt")}. Please wait.");

                Util.Message(tag, string.Empty, $"PID is {proc.Id}");
                Util.Message(tag, string.Empty, $"Waiting {_delaySecs} sec");
                Task.Delay(_delaySecs * 1000).Wait();

                // read
                var reader = new Reader();
                var ptr = reader.GetPointer(proc, Encoding.ASCII.GetBytes(Xsfp));
                if (ptr == IntPtr.Zero)
                {
                    Util.Message(Util.Tag("read"), "warn", "No potential FP values were found.");
                    return Retry();
                }

                // parse
                var handle = proc.Open();
                var result = Parse(new ExternalProcessMemory(handle), ptr);

                if (string.IsNullOrEmpty(result))
                {
                    Util.Message(Util.Tag("parse"), "warn", "No potential FP values were valid.");
                    return Retry();
                }

                // succeed!
                Cleanup();
                return result;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("Process has exited"))
                {
                    Util.Message(tag, "error", ex.Message);
                    Util.LogError(ex.Message);
                }

                return Retry();
            }
        }

        private string Retry()
        {
            _attemptCount++;

            if (_attemptCount <= _retryCount)
            {
                Util.Message(string.Empty, "retry", $"{(_attemptCount > 1 ? $"ATTEMPT {_attemptCount}/{_retryCount}" : string.Empty)}");
                Cleanup();
                return Find();
            }

            Util.Message(string.Empty, "failed", "No attempts remaining! Exiting.");
            Cleanup();
            return string.Empty;
        }

        private void Cleanup()
        {
            var procs = System.Diagnostics.Process.GetProcessesByName(Proc);
            if (procs.Length == 0)
                return;

            var hwndFw = Win32.FindWindow("Qt5QWindowIcon", "Bethesda.net Launcher");
            if (hwndFw != IntPtr.Zero)
            {
                Win32.SendMessage(hwndFw, 0x0010, IntPtr.Zero, IntPtr.Zero); // graceful
                return;
            }

            Util.Message(string.Empty, "warn", "Did not get handle to BNL, killing BNL forcibly");
            foreach (var proc in procs)
            {
                try
                {
                    proc.Kill(); // ungraceful
                    proc.Dispose();
                }
                catch (Exception) {/*eat: process already exited*/}
            }
        }

        private void Open()
        {
            Cleanup();

            var loc = @"%ProgramFiles(x86)%\Bethesda.net Launcher";
            var path = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Bethesda Softworks\Bethesda.net",
                "installLocation", loc);

            var proc = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $@"{(string.IsNullOrWhiteSpace(path) ? loc : path)}\{Proc}.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            proc.Start();
            while (string.IsNullOrEmpty(proc.MainWindowTitle))
            {
                Task.Delay(100).Wait();
                proc.Refresh();
            }

            var procs = System.Diagnostics.Process.GetProcessesByName(Proc);
            if (procs.Length == 0)
                throw new Exception("BNL processes were not found!");

            var statuses = GetProcessStatuses();
            foreach (var p in procs)
            {
                Win32.ShowWindow(p.MainWindowHandle, 2);
                _pStatus.Add(p, statuses[p.Id]);
            }
        }

        private string Read(IMemory mem, IntPtr ptr) => Encoding.ASCII.GetString(mem.Read(ptr, 50/*total fp length*/));

        private string Parse(IMemory mem, IntPtr ptr)
        {
            Util.Message(Util.Tag("parse"), string.Empty, $"Parsing potential value at {ptr}");
            var result = Read(mem, ptr).Replace(Xsfp, string.Empty).Trim();
            return Regex.IsMatch(result, "[a-fA-F0-9]{40}") ? result : string.Empty;
        }

        private Dictionary<int, bool> GetProcessStatuses()
        {
            var mos = new ManagementObjectSearcher($@"select CommandLine, ProcessId from Win32_Process where Name='{Proc}.exe'");
            var moc = mos.Get();
            var statuses = new Dictionary<int, bool>();
            var rendererFlags = new[] { "type", "gpu", "compositing", "extensions", "CEF", "raster", "mojo" };

            foreach (var m in moc)
            {
                var mObj = (ManagementObject)m;

                if (!int.TryParse(mObj["ProcessId"].ToString(), out var pid))
                    continue;

                if (rendererFlags.Any(rf => mObj["CommandLine"].ToString().Contains(rf)))
                {
                    statuses[pid] = true; // renderer proc
                    continue;
                }

                statuses[pid] = false; // non-renderer proc
            }

            return statuses;
        }

        private const string Proc = "BethesdaNetLauncher";
    }
}
