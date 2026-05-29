using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaCLI
{
    public static class ProjectHelper
    {
        public static bool IsGameDirectory() => File.Exists("rocketConfig.json");
        public static string GenerateEngineFile()
        {
            return "";
        }

        public static string GenerateMainFile()
        {
            return "local engine = require(\"engine\");";
        }
        public static string GenerateProjectFile()
        {
            return "{}";
        }
    }
}
