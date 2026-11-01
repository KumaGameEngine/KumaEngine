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
        UI,
        Audio
    }

    public static class AssetRetriver
    {
        static List<ISourceHandler> Sources = new();

        public static void AddAssetSource(ISourceHandler source) => Sources.Add(source);

        static void SourceThrow()
        {
            if (Sources.Count == 0)
                throw new Exception("No asset sources defined. Cannot retrive assets");
        }

        public static Stream OpenFile(AssetKind kind, string relative, FileMode mode, FileAccess access)
        {
            foreach (var item in Sources)
            {
                if (item.TryFetchFile(kind, relative, out var p))
                    return item.OpenFile(p, mode, access);
            }

            throw new FileNotFoundException($"Could not find '{Path.Combine(kind.ToString(), relative)}'");
        }

        public static Stream OpenRead(AssetKind kind, string relative) => 
            OpenFile(kind, relative, FileMode.Open, FileAccess.Read);

        public static byte[] ReadAllBytes(AssetKind kind, string relative)
        {
            var stream = OpenRead(kind, relative);

            byte[] buff = new byte[stream.Length];

            stream.ReadExactly(buff);

            stream.Close();

            return buff;
        }

        public static string ReadAlltext(AssetKind kind, string relative) =>
            Encoding.Default.GetString(ReadAllBytes(kind, relative));

        public static string[] GetFiles(AssetKind kind, string relative,string filter = "*.*", bool recursive = false)
        {
            SourceThrow();

            List<string> files = new();

            foreach (var item in Sources)
                files.AddRange(item.ListFiles(kind, relative, filter, recursive));

            return files.ToArray();
        }
    }
}
