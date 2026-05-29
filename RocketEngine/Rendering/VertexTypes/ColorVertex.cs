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
    public struct ColorVertex(Vector3 Position,uint Color) : IVertex
    {
        public Vector3 Position = Position;
        public Vector4 Color = new Vector4(
            ((Color >> 16) & 0xFF) / 255f,
            ((Color >> 8) & 0xFF) / 255f,
            (Color & 0xFF) / 255f,
            ((Color >> 24) & 0xFF) / 255f
        );

        public static VertexLayoutDescription Layout { get; set; } = new VertexLayoutDescription(
            new VertexElementDescription("Position",VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color",VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
        );
        public static uint Size { get; set; } = (3 * 4) + (4 * 4);
    }
}
