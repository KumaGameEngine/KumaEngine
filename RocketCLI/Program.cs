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

            var properties = args.Where(x=>x.StartsWith('-')).ToArray();
            var arguments = args.Where(x=>!x.StartsWith('-')).ToArray();

            switch (arguments[0].ToLower())
            {
                case "help":
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  help  | Show this help message");
                    Console.WriteLine("  debug | start the debug player");
                    Console.WriteLine("  new   | create a new project");
                    break;
                case "debug":
                    if (arguments.Length > 1)
                    {
                        var dir = Path.GetFullPath(arguments[1]);
                        if (!Directory.Exists(dir))
                        {
                            Console.WriteLine($"Directory '{dir}' does not exist");
                            return;
                        }

                        Environment.CurrentDirectory = Path.GetFullPath(arguments[1]);
                    }

                    if (!ProjectHelper.IsGameDirectory())
                    {
                        Console.WriteLine("Cannot debug in this directory");
                        return;
                    }

                    ProjectHelper.CopyDirectory(
                        Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!,"Data"),
                        "Data",true
                    );

                    SdlWindow window = new SdlWindow("KumaCLI debug");
                    Game instance = new Game(window);

                    var backend = PropertiesManager.GetBackend(properties);

                    Console.WriteLine("Using backend: " + backend);

                    window.Run(backend);
                    break;
                case "new":
                    if (arguments.Length < 2)
                    {
                        Console.WriteLine("Invalid usage");
                        Console.WriteLine();
                        Console.WriteLine($"{name} new <name> [arguments]");
                        return;
                    }

                    ProjectCreator.CreateProject(arguments[1]);
                    break;
                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
        }
    }
}
