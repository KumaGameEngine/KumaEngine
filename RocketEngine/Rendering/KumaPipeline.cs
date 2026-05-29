using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RocketModelScheme
    {
        public Matrix4x4 Model;

        public Vector3 NormalCol0;
        float _pad0;

        public Vector3 NormalCol1;
        float _pad1;

        public Vector3 NormalCol2;
        float _pad2;
        public RocketModelScheme(Matrix4x4 model)
        {
            Model = model;

            Matrix4x4 inverted;
            if (Matrix4x4.Invert(model, out inverted))
            {
                Matrix4x4 normalMatrix = Matrix4x4.Transpose(inverted);

                NormalCol0 = new Vector3(normalMatrix.M11, normalMatrix.M12, normalMatrix.M13);
                NormalCol1 = new Vector3(normalMatrix.M21, normalMatrix.M22, normalMatrix.M23);
                NormalCol2 = new Vector3(normalMatrix.M31, normalMatrix.M32, normalMatrix.M33);
            }
            else
            {
                NormalCol0 = new Vector3(model.M11, model.M12, model.M13);
                NormalCol1 = new Vector3(model.M21, model.M22, model.M23);
                NormalCol2 = new Vector3(model.M31, model.M32, model.M33);
            }

            _pad0 = 0.0f;
            _pad1 = 0.0f;
            _pad2 = 0.0f;
        }

        public const int Size = 112;
    }

    public class KumaPipeline
    {
        GraphicsPipelineDescription Descriptor;
        public Pipeline Pipeline;

        public List<TextureView> Textures { get; set; } = new();
        public ResourceSet Resources { get; set; } = null!;
        public Dictionary<string, RoketVertexElement> VertexDefinition { get; set; } = new();

        public static Dictionary<string, OutputDescription> SwapChains = new();

        public DeviceBuffer ModelBuffer;

        public KumaPipeline(ResourceFactory factory,GraphicsPipelineDescription pipelineDescription) 
        {
            Descriptor = pipelineDescription;
            Pipeline = factory.CreateGraphicsPipeline(ref Descriptor);

            ModelBuffer = factory.CreateBuffer(new(RocketModelScheme.Size, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        }

        public void Draw(CommandList list,Model model,KumaMaterial mat)
        {
            list.SetPipeline(Pipeline);

            list.SetGraphicsResourceSet(0, Resources);

            if (mat != null)
                for (int i = 0; i < mat.Resources.Count; i++) list.SetGraphicsResourceSet((uint)i + 1, mat.Resources[i]);

            list.SetVertexBuffer(0,model.VertexBuffer);
            list.SetIndexBuffer(model.IndexBuffer,IndexFormat.UInt32);

            list.DrawIndexed(model.IndexCount);
        }

        public static KumaPipeline FromSet(ResourceFactory factory,string set)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = [ new StringEnumConverter() ]
            };

            var defpath = Path.Combine("Shaders", set, "pipeline.json");

            if (!File.Exists(defpath)) throw new Exception("Could not find pipeline definition file");

            var result = JsonConvert.DeserializeObject<PipelineFile>(File.ReadAllText(defpath), settings);

            List<VertexElementDescription> layoutElements = new();
            foreach (var item in result.VertexDefinition)
                layoutElements.Add(new(item.Key,item.Value.ToFormat(),VertexElementSemantic.TextureCoordinate));

            List<ResourceLayoutElementDescription> resourceLayoutElementDescriptions = new();
            List<ResourceLayoutElementDescription> textureLayoutElementDescriptions = new();

            foreach (var item in result.Uniforms)
            {
                switch (item.Value)
                {
                    case RoketPipelineUniforms.CameraProjViewPerspective:
                    case RoketPipelineUniforms.ObjectModelMatrix:
                    case RoketPipelineUniforms.Lights:
                    case RoketPipelineUniforms.CameraPos:
                        resourceLayoutElementDescriptions.Add(
                            new ResourceLayoutElementDescription(item.Key, ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
                        );
                        break;
                    case RoketPipelineUniforms.LinearSamplerCube:
                    case RoketPipelineUniforms.LinearSampler2D:
                    case RoketPipelineUniforms.LinearSampler3D:
                        textureLayoutElementDescriptions.AddRange(
                            new ResourceLayoutElementDescription(item.Key+"Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                            new ResourceLayoutElementDescription(item.Key+"Samp", ResourceKind.Sampler, ShaderStages.Fragment)
                        );
                        break;
                    default:
                        break;
                }
            }

            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(resourceLayoutElementDescriptions.ToArray());
            ResourceLayout sharedLayout = factory.CreateResourceLayout(resourceLayoutDescription);

            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription(textureLayoutElementDescriptions.ToArray());
            ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

            var ret = new KumaPipeline(factory, new()
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.CounterClockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = [sharedLayout, textureLayout],
                ShaderSet = new ShaderSetDescription(
                    vertexLayouts: [new(layoutElements.ToArray())],
                    shaders: Shaders.FromSPIRV(factory, set)
                ),
                Outputs = SwapChains[result.Output]
            });

            ret.VertexDefinition = result.VertexDefinition;

            List<BindableResource> bindableResources = new();

            foreach (var item in result.Uniforms)
            {
                switch (item.Value)
                {
                    case RoketPipelineUniforms.CameraProjViewPerspective:
                        bindableResources.Add(DefinitionFile.Game.Camera._cameraProjViewBuffer);
                        break;
                    case RoketPipelineUniforms.ObjectModelMatrix:
                        bindableResources.Add(ret.ModelBuffer);
                        break;
                    case RoketPipelineUniforms.Lights:
                        bindableResources.Add(KumaScene.PointLightBuffer);
                        break;
                    case RoketPipelineUniforms.CameraPos:
                        bindableResources.Add(DefinitionFile.Game.Camera._cameraPosBuffer);
                        break;
                    default:
                        break;
                }
            }

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(sharedLayout, bindableResources.ToArray());
            var _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            ret.Resources = _sharedResourceSet;

            return ret;
        }
    }

    public static class RocketCommandListExtensions
    {
        public static void SetPipeline(this CommandList list, KumaPipeline pipeline) => list.SetPipeline(pipeline.Pipeline);

        public static VertexElementFormat ToFormat(this RoketVertexElement element) => element switch
        {
            RoketVertexElement.UV => VertexElementFormat.Float2,
            RoketVertexElement.UVW => VertexElementFormat.Float3,
            RoketVertexElement.Position => VertexElementFormat.Float3,
            RoketVertexElement.Normal => VertexElementFormat.Float3,
            RoketVertexElement.Color => VertexElementFormat.Float4,
            RoketVertexElement.Tangent => VertexElementFormat.Float3,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public enum RoketPipelineUniforms
    {
        CameraProjViewPerspective,
        CameraPos,
        ObjectModelMatrix,
        LinearSamplerCube,
        LinearSampler2D,
        LinearSampler3D,
        Lights
    }
    public enum RoketVertexElement : byte
    {
        UV,
        UVW,
        Position,
        Normal,
        Color,
        Tangent,
    }

    public class PipelineFile
    {
        public string Output = "MainSwapchain";

        public Dictionary<string, RoketPipelineUniforms> Uniforms = new();
        public Dictionary<string, RoketVertexElement> VertexDefinition = new();
    }
}
