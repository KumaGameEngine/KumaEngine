using KumaEngine;
using KumaEngine.SourceHandlers;
using KumaEngine.Windowing;
using Newtonsoft.Json;
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

                    var sources = JsonConvert.DeserializeObject<ProjectFile>(File.ReadAllText("kumaConfig.json"));

                    foreach (var item in sources.Sources)
                    {
                        switch (item.Kind)
                        {
                            case AssetSourceKind.Directory:
                                AssetRetriver.AddAssetSource(
                                    new DirectorySourceHandler(
                                        Path.GetFullPath(item.Path)
                                    )
                                );
                                break;
                            case AssetSourceKind.PackFile:
                                AssetRetriver.AddAssetSource(
                                    new RePakSourceHandler(
                                        Path.GetFullPath(item.Path),
                                        item.PackKey.Length > 0 ? item.PackKey : sources.DefaultPackKey
                                    )
                                );
                                break;
                            case AssetSourceKind.GenericGroup:
                                foreach (var item1 in Directory.GetDirectories(item.Path))
                                {
                                    AssetRetriver.AddAssetSource(
                                        new DirectorySourceHandler(
                                            Path.GetFullPath(item1)
                                        )
                                    );
                                }
                                foreach (var item1 in Directory.GetFiles(item.Path, "*.pak", SearchOption.AllDirectories))
                                {
                                    AssetRetriver.AddAssetSource(
                                        new RePakSourceHandler(
                                            Path.GetFullPath(item1),
                                            item.PackKey.Length > 0 ? item.PackKey : sources.DefaultPackKey
                                        )
                                    );
                                }
                                break;
                            case AssetSourceKind.DirectoryGroup:
                                foreach (var item1 in Directory.GetDirectories(item.Path))
                                {
                                    AssetRetriver.AddAssetSource(
                                        new DirectorySourceHandler(
                                            Path.GetFullPath(item1)
                                        )
                                    );
                                }
                                break;
                            case AssetSourceKind.PackFileGroup:
                                foreach (var item1 in Directory.GetFiles(item.Path, "*.pak", SearchOption.AllDirectories))
                                {
                                    AssetRetriver.AddAssetSource(
                                        new RePakSourceHandler(
                                            Path.GetFullPath(item1),
                                            item.PackKey.Length > 0 ? item.PackKey : sources.DefaultPackKey
                                        )
                                    );
                                }
                                break;
                        }
                    }

                    AssetRetriver.AddAssetSource(
                        new DirectorySourceHandler(
                            Path.Combine(
                                Path.GetDirectoryName(Environment.ProcessPath)!, 
                                "Data"
                            )
                        )
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
