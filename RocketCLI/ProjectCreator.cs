using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaCLI
{
    public static class ProjectCreator
    {
        static List<IPathElement> ProjectStructure = new()
        {
            new DirectoryElement("Data",new()
            {
                new DirectoryElement("Textures"),
                new DirectoryElement("Models"),
                new DirectoryElement("UI"),
                new DirectoryElement("Materials"),
                new DirectoryElement("Fonts"),
                new DirectoryElement("Scripts",new()
                {
                    new FileElement("main.lua",ProjectHelper.GenerateMainFile()),
                    new FileElement("engine.lua",ProjectHelper.GenerateEngineFile())
                }),
                new DirectoryElement("Shaders"),
            }),
            new FileElement("rocketConfig.json",ProjectHelper.GenerateProjectFile())
        };

        public static void CreateProject(string name)
        {
            var dir = Path.GetDirectoryName(Environment.ProcessPath);

            Directory.CreateDirectory(name);

            foreach (var item in ProjectStructure)
            {
                if (item is DirectoryElement de)
                    DirectoryUtilities.TryCopyDirectory(
                        Path.Combine(dir!, de.Name), 
                        Path.Combine(name, de.Name)
                    );

                item.Create(name);
            }
        }
    }

    interface IPathElement
    {
        public void Create(string parent);
    }

    struct DirectoryElement(string name, params List<IPathElement> children) : IPathElement
    {
        public string Name { get; set; } = name;

        public void Create(string parent)
        {
            var np = Path.Combine(parent, name);

            Directory.CreateDirectory(np);

            foreach (var item in children) item.Create(np);
        }
    }

    struct FileElement(string name, string content) : IPathElement
    {
        public void Create(string parent)
        {
            var np = Path.Combine(parent, name);
            File.WriteAllText(np, content);
        }
    }
}
