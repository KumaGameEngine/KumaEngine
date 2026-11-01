using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaCLI
{
    public static class ProjectHelper
    {
        public static bool IsGameDirectory() => File.Exists("kumaConfig.json");
        public static string GenerateEngineFile()
        {
            HttpClient client = new();
            return client.GetStringAsync(
                "https://raw.githubusercontent.com/KumaGameEngine/Docs/refs/heads/main/source/engine.lua"
            ).GetAwaiter().GetResult();
        }

        public static string GenerateMainFile()
        {
            return "local engine = require(\"engine\");";
        }
        public static string GenerateProjectFile() => JsonConvert.SerializeObject(new ProjectFile()
        {
            Sources = [
                new() {
                    Kind = AssetSourceKind.Directory,
                    Path = "./Data"
                }
            ]
        }, Formatting.Indented);

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            var dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
