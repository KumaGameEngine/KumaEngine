using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Rendering
{
    public class KumaPointLight(Vector3 pos, float radius, Vector3 Color, float Intensity)
    {
        public Vector3 Position = pos;
        public float Radius = radius;
        public Vector3 Color = Color;
        public float Intensity = Intensity;
        Vector3 direction;
        float spotAngle;

        public RocketPointLightScheme reference { get => new()
        {
            Position = Position,
            Radius = Radius,
            Color = new Vector4(Color, Intensity)
        };}
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RocketPointLightScheme
    {
        public Vector3 Position;
        public float Radius;
        public Vector4 Color;
        Vector3 direction;
        float spotAngle;

        public const int Size = (3*4) + 4 + (4*4) + (3*4) + 4;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LightUploadScheme
    {
        public const int MAX_POINT_LIGHTS = 32;

        [System.Runtime.CompilerServices.InlineArray(MAX_POINT_LIGHTS)]
        public struct PointLightArray
        {
            private RocketPointLightScheme _element;
        }

        public PointLightArray PointLights;

        public uint PointLightCount;
        float _pad0;
        float _pad1;
        float _pad2;

        public static int Size = Marshal.SizeOf<LightUploadScheme>();
    }
}
