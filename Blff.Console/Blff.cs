using System;

namespace blff
{
    // blff: bnlauncher (hardware) fingerprint finder, syncore <syncore@syncore.org> 2018.
    public class Blff
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"blff {LibBlff.Version}: bnlauncher(hardware) fingerprint finder");
            Console.WriteLine($"syncore <syncore@syncore.org> 2018.{Environment.NewLine}");
            new LibBlff().Run(args);
        }
    }
}
