using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaCLI
{
    public class ProjectFile
    {
        public string DefaultPackKey { get; set; } = "";

        public List<AssetSource> Sources { get; set; } = new();
    }

    public class AssetSource
    {
        public AssetSourceKind Kind { get; set; }

        public string PackKey { get; set; } = "";

        public string Path { get; set; } = "";
    }

    public enum AssetSourceKind
    {
        Directory,
        PackFile,
        GenericGroup,
        DirectoryGroup,
        PackFileGroup,
    }
}
