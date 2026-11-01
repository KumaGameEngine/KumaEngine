using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine
{
    public interface ISourceHandler
    {
        public bool TryFetchFile(AssetKind kind, string relative, out string path);
        public Stream OpenFile(string path, FileMode mode, FileAccess access);

        public string[] ListFiles(AssetKind kind, string relative, string filter = "*.*", bool recursive = false);
    }
}
