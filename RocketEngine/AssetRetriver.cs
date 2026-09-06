using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine
{
    public enum AssetKind
    {
        Fonts,
        Materials,
        Models,
        Scripts,
        Shaders,
        Swapchains,
        Textures,
        UI
    }

    public static class AssetRetriver
    {
        static List<string> Sources = new();

        static Dictionary<string, string> FileFetchCache = new();

        public static void AddAssetSource(string source) => Sources.Add(source);

        static void SourceThrow()
        {
            if (Sources.Count == 0)
                throw new Exception("No asset sources defined. Cannot retrive assets");
        }

        public static string FetchAssetPath(AssetKind kind, string relative)
        {
            SourceThrow();

            var rpath = Path.Combine("Data", kind.ToString(), relative);

            if (FileFetchCache.TryGetValue(rpath, out var op))
                return op;

            foreach (var item in Sources)
            {
                var path = Path.Combine(item, rpath);
                if (File.Exists(path))
                {
                    FileFetchCache.Add(rpath, path);
                    return path;
                }
            }

            throw new FileNotFoundException($"No defined asset source contains {relative}");
        }

        public static string[] GetFiles(AssetKind kind, string relative,string filter = "*.*", bool recursive = false)
        {
            SourceThrow();

            List<string> files = new();
            var rpath = Path.Combine("Data", kind.ToString(), relative);

            foreach (var item in Sources)
            {
                var path = Path.Combine(item, rpath);

                if (Directory.Exists(path)) files.AddRange(
                    Directory.GetFiles(
                        path,filter,
                        recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                    )
                );
            }

            return files.ToArray();
        }

        public static string[] GetDirectories(AssetKind kind, string relative, string filter = "*", bool recursive = false)
        {
            SourceThrow();

            List<string> files = new();
            var rpath = Path.Combine("Data", kind.ToString(), relative);

            foreach (var item in Sources)
            {
                var path = Path.Combine(item, rpath);

                if (Directory.Exists(path)) files.AddRange(
                    Directory.GetDirectories(
                        path, filter,
                        recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                    )
                );
            }

            return files.ToArray();
        }
    }
}
