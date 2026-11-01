using RePAKLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KumaEngine.SourceHandlers
{
    public class RePakSourceHandler : ISourceHandler
    {
        RePAKVFS vfs;

        public RePakSourceHandler(string pakFilePath, string key)
        {
            var fs = File.OpenRead(pakFilePath);

            vfs = RePAK.Decrypt(fs, key);
        }

        public string[] ListFiles(
            AssetKind kind, string relative, string filter = "*.*",
            bool recursive = false, bool relativePath = false
        ){
            string prefix = Path.Combine(kind.ToString(), relative).Replace('\\', '/').Trim('/');
            prefix = prefix.Length == 0 ? string.Empty : prefix + "/";

            string pattern = (string.IsNullOrEmpty(filter) || filter == "*.*") ? "*" : filter;
            var regex = new Regex(
                "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var results = new List<string>();

            foreach (var key in vfs.FileTable.Keys)
            {
                string path = key.Replace('\\', '/');

                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string remainder = path.Substring(prefix.Length);

                if (!recursive && remainder.Contains('/'))
                    continue;

                string fileName = remainder.Substring(remainder.LastIndexOf('/') + 1);
                if (!regex.IsMatch(fileName))
                    continue;

                if (relativePath)
                    path = remainder;

                results.Add(path);
            }

            return results.ToArray();
        }

        public Stream OpenFile(string path, FileMode mode, FileAccess access)
        {
            if (mode != FileMode.Open || access != FileAccess.Read)
                throw new NotSupportedException("RePAK source handler only supports reading files");

            return vfs.GetFileStream(path.Replace('\\', '/'));
        }

        public bool TryFetchFile(AssetKind kind, string relative, out string path)
        {
            path = Path.Combine(kind.ToString(), relative).Replace('\\', '/');

            return vfs.Exists(path);
        }
    }
}
