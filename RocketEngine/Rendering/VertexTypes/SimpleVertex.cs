using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering.VertexTypes
{
    [StructLayout(LayoutKind.Sequential,Pack = 1)]
    public struct SimpleVertex(Vector3 Position) : IVertex
    {
        public Vector3 Position = Position;

        public static VertexLayoutDescription Layout { get; set; } = new VertexLayoutDescription(
            new VertexElementDescription("Position",VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3)
        );
        public static uint Size { get; set; } = (3 * 4) + (4 * 4);
    }
}
