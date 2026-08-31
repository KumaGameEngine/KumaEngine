using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Rendering
{
    public class KumaLight(Vector3 pos, float radius, Vector3 Color, float Intensity)
    {
        public Vector3 Position = pos;
        public float Radius = radius;
        public Vector3 Color = Color;
        public float Intensity = Intensity;
        public Vector3 Direction;
        public float SpotAngle;

        public RocketLightScheme reference { 
            get => new()
            {
                Position = Position,
                Radius = Radius,
                Color = new Vector4(Color, Intensity),
                Direction = Camera.EulerToDirection(Direction),
                SpotAngle = SpotAngle
            };
        }
    }

    public class KumaSun(Vector3 Direction, Vector3 Color, float Intensity)
    {
        public Vector3 Direction = Direction;
        public Vector3 Color = Color;
        public float Intensity = Intensity;

        public RocketSunScheme reference
        {
            get => new()
            {
                Direction = Camera.EulerToDirection(Direction),
                Color = new Vector4(Color, Intensity)
            };
        }

        public static readonly KumaSun Default = new(new(45,-45,0),Vector3.One,1);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RocketLightScheme
    {
        public Vector3 Position;
        public float Radius;
        public Vector4 Color;
        public Vector3 Direction;
        public float SpotAngle;

        public const int Size = (3*4) + 4 + (4*4) + (3*4) + 4;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RocketSunScheme
    {
        public Vector3 Direction;
        uint _pad;

        public Vector4 Color;

        public const int Size = (3 * 4) + (4 * 4);
    }

    [StructLayout(LayoutKind.Sequential,Pack = 1)]
    public unsafe struct LightUploadScheme
    {
        public const int MAX_POINT_LIGHTS = 32;

        [System.Runtime.CompilerServices.InlineArray(MAX_POINT_LIGHTS)]
        public struct PointLightArray
        {
            private RocketLightScheme _element;
        }

        public PointLightArray Lights;

        public uint PointLightCount;
        Vector3 _pad;

        public RocketSunScheme Sun;

        public static int Size = Marshal.SizeOf<LightUploadScheme>();
    }
}
