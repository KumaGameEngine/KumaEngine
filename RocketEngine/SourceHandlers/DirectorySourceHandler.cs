using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.SourceHandlers
{
    public class DirectorySourceHandler : ISourceHandler
    {
        public string Directory { get; }

        public DirectorySourceHandler(string dir)
        {
            Directory = dir;
        }

        public string[] ListFiles(AssetKind kind, string relative, string filter = "*.*", bool recursive = false)
        {
            var dir = Path.Combine(Directory, kind.ToString(), relative);

            if (!System.IO.Directory.Exists(dir)) return [];

            return System.IO.Directory.GetFiles(
                dir, filter,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            );
        }

        public Stream OpenFile(string path, FileMode mode, FileAccess access) =>
            File.Open(path, mode, access);

        public bool TryFetchFile(AssetKind kind, string relative, out string path)
        {
            path = Path.Combine(Directory, kind.ToString(), relative);

            return File.Exists(path);
        }
    }
}
