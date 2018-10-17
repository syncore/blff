using System;
using System.IO;

namespace blff
{
    public static class Util
    {
        public static void Message(string tag, string msgType, string msg)
        {
            Console.WriteLine($"{(string.IsNullOrWhiteSpace(tag) ? string.Empty : $"{tag} ")}" +
                              $"{(string.IsNullOrWhiteSpace(msgType) ? "\t" : $"{msgType.ToUpperInvariant()}\t")} {msg}");
        }

        public static string Tag(string tag) => $"{tag.ToUpper()}\t";

        public static void LogError(string error) => WriteFile($"{DateTime.Now.ToShortTimeString()}\t[ERROR]\t{error}",
            LibBlff.ErrorLog, true);

        public static void WriteFile(string content, string fileName, bool append = false)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (append)
                File.AppendAllText(path, $"{content}{Environment.NewLine}");
            else
                File.WriteAllText(path, content);
        }
    }
}
