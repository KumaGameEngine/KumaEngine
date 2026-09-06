using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace KumaEngine.Rendering
{
    public static class Shaders
    {
        static string PreProcess(string file)
        {
            var src = Regex.Replace(File.ReadAllText(file), @"/\*.*?\*/|//[^\n]*", "", RegexOptions.Singleline).Trim();
            var sb = new StringBuilder();

            int linesCount = 0;

            foreach (var line in src.Split('\n'))
            {
                if (line.ToLowerInvariant().StartsWith("#kumainclude "))
                {
                    var s = line.Substring("#kumainclude ".Length).Trim();

                    if (!(s.StartsWith('<') || s.StartsWith('"')) || !(s.EndsWith('>') || s.EndsWith('"'))) 
                        throw new Exception("Invalid #kumainclude directive");

                    if ((s[0] == '<' && s[^1] != '>') || (s[0] == '"' && s[^1] != '"')) 
                        throw new Exception($"Line {linesCount}: ending character does not match starting character");

                    var path = s[1..^1];

                    var ext = Path.HasExtension(path) ? Path.GetExtension(path) : ".glsl";
                    sb.AppendLine(PreProcess(AssetRetriver.FetchAssetPath(AssetKind.Shaders, Path.GetFileNameWithoutExtension(path) + ext)));
                }
                else sb.AppendLine(line);

                linesCount++;
            }
            return sb.ToString();
        }

        static byte[] PreProcessBytes(string file) => Encoding.Default.GetBytes(PreProcess(file));

        public static Shader[] FromSPIRVVertFrag(ResourceFactory factory, string ShaderPackName, string vert,string frag)
        {
            var vertex = new ShaderDescription(
                ShaderStages.Vertex, 
                PreProcessBytes(AssetRetriver.FetchAssetPath(AssetKind.Shaders, Path.Combine(ShaderPackName, vert))),
                "main"
            );

            var fragment = new ShaderDescription(
                ShaderStages.Fragment,
                frag is null ? GetDummyFragVert() : PreProcessBytes(AssetRetriver.FetchAssetPath(AssetKind.Shaders, Path.Combine(ShaderPackName, frag))),
                "main"
            );

            return factory.CreateFromSpirv(vertex, fragment, new());
        }
        public static Shader FromSPIRVCompute(ResourceFactory factory, string ShaderPackName, string compute) =>
            factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Compute, PreProcessBytes(AssetRetriver.FetchAssetPath(AssetKind.Shaders, Path.Combine(ShaderPackName, compute))),"main"),
                new CrossCompileOptions()
            );

        static byte[] GetDummyFragVert() => 
            Encoding.Default.GetBytes("#version 450\nvoid main() {}");
    }
}
