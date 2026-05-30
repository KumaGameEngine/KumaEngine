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
                    sb.AppendLine(PreProcess(Path.Combine("Shaders", Path.GetFileNameWithoutExtension(path) + ext)));
                }
                else sb.AppendLine(line);

                linesCount++;
            }
            return sb.ToString();
        }

        static byte[] PreProcessBytes(string file) => Encoding.Default.GetBytes(PreProcess(file));

        public static Shader[] FromSPIRV(ResourceFactory factory, string ShaderPackName) =>
            factory.CreateFromSpirv(
                new(ShaderStages.Vertex, PreProcessBytes(Path.Combine("Shaders",ShaderPackName,"vertex.glsl")),"main"),
                new(ShaderStages.Fragment, PreProcessBytes(Path.Combine("Shaders",ShaderPackName,"fragment.glsl")),"main"),
                new()
            );
    }
}
