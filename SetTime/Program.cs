using System;

namespace SetTime
{
    class Program
    {
        static bool wait = false;
        static bool help = false;
        static bool badArgs = false;

        static int Main(string[] args)
        {
            ConsoleColor defaultColour = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"TimeMachine v{System.Reflection.Assembly.GetEntryAssembly().GetName().Version}");
            Console.ForegroundColor = defaultColour;

            PaseParams(args);
            if (badArgs)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Bad Arguments");
                Console.ForegroundColor = defaultColour;
                ShowHelp();
                return -1;
            }

            if (help)
            {
                ShowHelp();
                return 0;
            }

            Console.WriteLine("( -? for help )");
            Console.ForegroundColor = ConsoleColor.Green;
            new Machine().Run();
            Console.ForegroundColor = defaultColour;

            if (wait)
            {
                Console.WriteLine("Press any key to finish");
                Console.ReadKey();
            }

            return 0;
        }

        private static void ShowHelp()
        {
            Console.WriteLine($"Usage: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} [-?|-h|--help] [-w|--wait]");
            Console.WriteLine(" -? or -h or --help  Show this help");
            Console.WriteLine(" -w or --wait        Pause for user action after setting time");
        }

        private static void PaseParams(string[] args)
        {
            help = (args.Length == 1 &&
                (args[0].ToLower() == "-h" ||
                 args[0].ToLower() == "-?" ||
                 args[0].ToLower() == "--help")
            );

            wait = (args.Length == 1 &&
                (args[0].ToLower() == "-w" ||
                 args[0].ToLower() == "--wait")
            );

            if (args.Length > 1 || (args.Length== 1 && !wait && !help))
            {
                badArgs = true;
            }

        }
    }
}
