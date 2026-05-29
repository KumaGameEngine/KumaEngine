using KumaEngine;
using KumaEngine.Windowing;
using System.Reflection;

namespace KumaCLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var name = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var rocketversion = typeof(Game).Assembly.GetName().Version;

            if (args.Length == 0)
            {
                Console.WriteLine($"KumaCLI     v{version!.ToString(3)}");
                Console.WriteLine($"KumaEngine  v{rocketversion!.ToString(3)}");

                Console.WriteLine();

                Console.WriteLine($"{name} <command> [arguments]");

                Console.WriteLine();

                Console.WriteLine($"Run '{name} help' to get a list of all commands");

                return;
            }

            switch (args[0].ToLower())
            {
                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  help  | Show this help message");
                    Console.WriteLine("  debug | start the debug player");
                    Console.WriteLine("  new   | create a new project");
                    break;
                case "debug":
                    if (!ProjectHelper.IsGameDirectory())
                    {
                        Console.WriteLine("Cannot debug in this directory");
                        return;
                    }

                    SdlWindow window = new SdlWindow("KumaCLI debug");
                    Game instance = new Game(window);

                    window.Run();
                    break;
                case "new":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Invalid usage");
                        Console.WriteLine();
                        Console.WriteLine($"{name} new <name> [arguments]");
                        return;
                    }

                    ProjectCreator.CreateProject(args[1]);
                    break;
                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
        }
    }
}
