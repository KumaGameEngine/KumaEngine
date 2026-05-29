using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace KumaEngine.Rendering
{
    public static class Shaders
    {
        public static Shader[] FromSPIRV(ResourceFactory factory, string ShaderPackName) =>
            factory.CreateFromSpirv(
                new(ShaderStages.Vertex,File.ReadAllBytes(Path.Combine("Shaders",ShaderPackName,"vertex.glsl")),"main"),
                new(ShaderStages.Fragment,File.ReadAllBytes(Path.Combine("Shaders",ShaderPackName,"fragment.glsl")),"main"),
                new()
            );
    }
}
