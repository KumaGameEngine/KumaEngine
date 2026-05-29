using Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class Model : IDisposable
    {
        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }
        public uint IndexCount { get; private set; }

        public static Model Create<T>(GraphicsDevice gd, ResourceFactory factory, T[] verticies, uint[] indicies) where T : unmanaged, IVertex
        {
            var mdl = new Model();
            mdl.VertexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)verticies.Length * T.Size, BufferUsage.VertexBuffer));
            gd.UpdateBuffer(mdl.VertexBuffer, 0, verticies);

            mdl.IndexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)indicies.Length * sizeof(uint), BufferUsage.IndexBuffer));
            gd.UpdateBuffer(mdl.IndexBuffer, 0, indicies);

            mdl.IndexCount = (uint)indicies.Length;

            return mdl;
        }

        public static Model CreateBytes(GraphicsDevice gd, ResourceFactory factory, byte[] verticies, uint[] indicies)
        {
            var mdl = new Model();
            mdl.VertexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)verticies.Length, BufferUsage.VertexBuffer));
            gd.UpdateBuffer(mdl.VertexBuffer, 0, verticies);

            mdl.IndexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)indicies.Length * sizeof(uint), BufferUsage.IndexBuffer));
            gd.UpdateBuffer(mdl.IndexBuffer, 0, indicies);

            mdl.IndexCount = (uint)indicies.Length;

            return mdl;
        }

        public static List<Model> FromFile(GraphicsDevice gd, ResourceFactory factory,KumaPipeline pipeline, string model)
        {
            var ctx = new AssimpContext();

            var p = Path.Combine("Data", "Models",model);

            if (!File.Exists(p)) throw new Exception("Invalid model file");

            Scene s = ctx.ImportFile(
                p,
                PostProcessSteps.Triangulate | 
                PostProcessSteps.OptimizeMeshes | 
                PostProcessSteps.GenerateSmoothNormals | 
                PostProcessSteps.CalculateTangentSpace
            );

            List<Model> Models = new();

            var channels = new List<IMeshChannel>();

            foreach (var item in pipeline.VertexDefinition)
            {
                switch (item.Value)
                {
                    case RoketVertexElement.UV:
                        channels.Add(new UVMeshChannel());
                        break;
                    case RoketVertexElement.UVW:
                        channels.Add(new UVWMeshChannel());
                        break;
                    case RoketVertexElement.Position:
                        channels.Add(new PositionMeshChannel());
                        break;
                    case RoketVertexElement.Normal:
                        channels.Add(new NormalsMeshChannel());
                        break;
                    case RoketVertexElement.Color:
                        channels.Add(new ColorMeshChannel());
                        break;
                    case RoketVertexElement.Tangent:
                        channels.Add(new TangentsMeshChannel());
                        break;
                    default:
                        break;
                }
            }

            List<byte> data = new();

            foreach (var item in s.Meshes)
            {
                foreach (var item1 in channels) item1.GatherData(item,0);

                data.Clear();

                for (uint i = 0; i < item.Vertices.Count; i++)
                    foreach (var item1 in channels) data.AddRange(item1.GetBytes(i));

                Models.Add(CreateBytes(gd,factory,data.ToArray(),item.GetUnsignedIndices().ToArray()));
            }

            return Models;
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }

    public interface IMeshChannel
    {
        public void GatherData(Mesh mesh, int channelid);
        public byte[] GetBytes(uint vertex);
    }

    public class PositionMeshChannel : IMeshChannel
    {
        List<Vector3> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.Vertices;
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X , Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class NormalsMeshChannel : IMeshChannel
    {
        List<Vector3> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.Normals;
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class TangentsMeshChannel : IMeshChannel
    {
        List<Vector3> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.Tangents;
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class UVWMeshChannel : IMeshChannel
    {
        List<Vector3> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.TextureCoordinateChannels[channelid];
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class UVMeshChannel : IMeshChannel
    {
        List<Vector3> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.TextureCoordinateChannels[channelid];
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y};
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class ColorMeshChannel : IMeshChannel
    {
        List<Vector4> Points = new();

        public void GatherData(Mesh mesh, int channelid)
        {
            Points = mesh.VertexColorChannels[channelid];
        }

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z, Points[(int)vertex].W };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
}
