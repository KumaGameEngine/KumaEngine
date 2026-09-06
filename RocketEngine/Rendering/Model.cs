using Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class Model : IDisposable
    {
        public Guid ModelID { get; } = new();
        public DeviceBuffer IndexBuffer { get; private set; }

        public VertexDescriptionSet Vertices { get; set; } = new();
        public List<uint> Indices { get; set; } = new();

        Dictionary<KumaPipeline, DeviceBuffer> _pipelineVertexCache = new();

        public Model()
        {
            IndexBuffer = null!;
            Vertices = new() 
            {
                UVWLayers = [],
                Tangents = [],
                ColorLayers = [],
                Normals = [],
                Vertices = []
            };
        }

        public void Invalidate()
        {
            foreach (var item in _pipelineVertexCache)
                item.Value.Dispose();

            _pipelineVertexCache.Clear();
        }

        public DeviceBuffer GetCompiledVerticies(GraphicsDevice gd, ResourceFactory factory, KumaPipeline pipeline)
        {
            if (IndexBuffer == null || Indices.Count * sizeof(uint) != IndexBuffer!.SizeInBytes)
            {
                IndexBuffer?.Dispose();
                IndexBuffer = factory.CreateBuffer(
                    new BufferDescription(
                        (uint)Indices.Count * sizeof(uint), 
                        BufferUsage.IndexBuffer
                    )
                );

                gd.UpdateBuffer(IndexBuffer, 0, Indices.ToArray());

                Invalidate();
            }

            if (_pipelineVertexCache.TryGetValue(pipeline, out var buffer))
                return buffer;

            Dictionary<KumaVertexElement, int> vertexLayers = new();

            List<IMeshChannel> builtChannels = new();

            foreach (var item in pipeline.VertexDefinition)
            {
                if (!vertexLayers.ContainsKey(item.Value))
                    vertexLayers[item.Value] = 0;

                switch (item.Value)
                {
                    case KumaVertexElement.UV:
                        builtChannels.Add(
                            new UVMeshChannel(
                                Vertices, 
                                vertexLayers[item.Value] + 
                                (vertexLayers.ContainsKey(KumaVertexElement.UVW) ? vertexLayers[KumaVertexElement.UVW] : 0)
                            )
                        );
                        break;
                    case KumaVertexElement.UVW:
                        builtChannels.Add(
                            new UVWMeshChannel(
                                Vertices, 
                                (vertexLayers.ContainsKey(KumaVertexElement.UV) ? vertexLayers[KumaVertexElement.UV] : 0) + 
                                vertexLayers[item.Value]
                            )
                        );
                        break;
                    case KumaVertexElement.Position:
                        builtChannels.Add(new PositionMeshChannel(Vertices));
                        break;
                    case KumaVertexElement.Normal:
                        builtChannels.Add(new NormalsMeshChannel(Vertices));
                        break;
                    case KumaVertexElement.Color:
                        builtChannels.Add(new ColorMeshChannel(Vertices, vertexLayers[item.Value]));
                        break;
                    case KumaVertexElement.Tangent:
                        builtChannels.Add(new TangentsMeshChannel(Vertices));
                        break;
                }

                vertexLayers[item.Value]++;
            }

            List<byte> compiledData = new();

            for (uint i = 0; i < Vertices.Vertices.Count; i++)
            {
                foreach (var item in builtChannels)
                    compiledData.AddRange(item.GetBytes(i));
            }

            DeviceBuffer db = factory.CreateBuffer(
                new BufferDescription(
                    (uint)compiledData.Count, 
                    BufferUsage.VertexBuffer
                )
            );
            gd.UpdateBuffer(db, 0, compiledData.ToArray());

            _pipelineVertexCache.Add(pipeline,db);

            return db;
        }

        public static List<Model> FromFile(string model)
        {
            var ctx = new AssimpContext();

            var p = AssetRetriver.FetchAssetPath(AssetKind.Models, model);

            if (!File.Exists(p)) throw new Exception("Invalid model file");

            Scene s = ctx.ImportFile(
                p,
                PostProcessSteps.Triangulate | 
                PostProcessSteps.OptimizeMeshes | 
                PostProcessSteps.GenerateSmoothNormals | 
                PostProcessSteps.CalculateTangentSpace
            );

            List<Model> Models = new();

            foreach (var item in s.Meshes)
            {
                var mdl = new Model();
                mdl.Vertices = new() 
                {
                    Vertices = item.Vertices,
                    ColorLayers = item.VertexColorChannels.ToList(),
                    Normals = item.Normals,
                    Tangents = item.Tangents,
                    UVWLayers = item.TextureCoordinateChannels.ToList(),
                };

                mdl.Indices = item.GetUnsignedIndices().ToList();

                Models.Add(mdl);
            }

            return Models;
        }

        public void Dispose()
        {
            Invalidate();
            IndexBuffer.Dispose();
        }
    }

    public struct VertexDescriptionSet
    {
        public List<Vector3> Vertices;
        public List<Vector3> Normals;
        public List<Vector3> Tangents;

        public List<List<Vector4>> ColorLayers;
        public List<List<Vector3>> UVWLayers;
    }

    public interface IMeshChannel
    {
        public byte[] GetBytes(uint vertex);
    }

    public class PositionMeshChannel(VertexDescriptionSet mesh) : IMeshChannel
    {
        List<Vector3> Points = mesh.Vertices;

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X , Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class NormalsMeshChannel(VertexDescriptionSet mesh) : IMeshChannel
    {
        List<Vector3> Points = mesh.Normals;

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class TangentsMeshChannel(VertexDescriptionSet mesh) : IMeshChannel
    {
        List<Vector3> Points = mesh.Tangents;

        public byte[] GetBytes(uint vertex)
        {
            Span<float> floats = stackalloc float[] { Points[(int)vertex].X, Points[(int)vertex].Y, Points[(int)vertex].Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class UVWMeshChannel(VertexDescriptionSet mesh, int channelid) : IMeshChannel
    {
        List<Vector3> Points = mesh.UVWLayers.Count == 0 ? 
            new(mesh.Vertices.Count) : 
            mesh.UVWLayers[Math.Min(mesh.UVWLayers.Count, channelid)];

        public byte[] GetBytes(uint vertex)
        {
            Vector3 point = vertex >= Points.Count ? Vector3.Zero : Points[(int)vertex];

            Span<float> floats = stackalloc float[] { point.X, point.Y, point.Z };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class UVMeshChannel(VertexDescriptionSet mesh, int channelid) : IMeshChannel
    {
        List<Vector3> Points = mesh.UVWLayers.Count == 0 ?
            new(mesh.Vertices.Count) :
            mesh.UVWLayers[Math.Min(mesh.UVWLayers.Count, channelid)];

        public byte[] GetBytes(uint vertex)
        {
            Vector3 point = vertex >= Points.Count ? Vector3.Zero : Points[(int)vertex];

            Span<float> floats = stackalloc float[] { point.X, point.Y};
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
    public class ColorMeshChannel(VertexDescriptionSet mesh, int channelid) : IMeshChannel
    {
        List<Vector4> Points = mesh.ColorLayers.Count == 0 ?
            new(mesh.Vertices.Count) :
            mesh.ColorLayers[Math.Min(mesh.ColorLayers.Count, channelid)];

        public byte[] GetBytes(uint vertex)
        {
            Vector4 point = vertex >= Points.Count ? Vector4.One : Points[(int)vertex];

            Span<float> floats = stackalloc float[] { point.X, point.Y, point.Z, point.W };
            Span<byte> bytes = MemoryMarshal.AsBytes(floats);

            return bytes.ToArray();
        }
    }
}
