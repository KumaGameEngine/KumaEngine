using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using ResourceSet = Veldrid.ResourceSet;

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
        public List<KumaPass> Passes = new();
        public DeviceBuffer ModelBuffer;
        public Dictionary<string, RoketVertexElement> VertexDefinition { get; set; } = new();

        static Framebuffer PrevSwapchain = null!;

        public KumaPipeline(ResourceFactory factory)
        {
            ModelBuffer = factory.CreateBuffer(new(RocketModelScheme.Size, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        }

        public static KumaPipeline FromSet(ResourceFactory factory, string set)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = [new StringEnumConverter()]
            };

            var defpath = Path.Combine("Data","Shaders", set, "pipeline.json");

            if (!File.Exists(defpath)) throw new Exception("Could not find pipeline definition file");

            var result = JsonConvert.DeserializeObject<PipelineFile>(File.ReadAllText(defpath), settings);

            List<VertexElementDescription> layoutElements = new();
            foreach (var item in result.VertexDefinition)
                layoutElements.Add(new(item.Key, item.Value.ToFormat(), VertexElementSemantic.TextureCoordinate));

            var pipeline = new KumaPipeline(factory);

            pipeline.VertexDefinition = result.VertexDefinition;

            foreach (var item in result.Passes)
                pipeline.Passes.Add(KumaPass.FromFile(factory,set,item,pipeline,layoutElements));

            return pipeline;
        }

        public void Draw(CommandList list, Model model, KumaMaterial mat)
        {
            foreach (var item in Passes)
            {
                if (PrevSwapchain != item.swapchain.Framebuffer)
                {
                    list.SetFramebuffer(item.swapchain.Framebuffer);
                    PrevSwapchain = item.swapchain.Framebuffer;
                }

                list.SetPipeline(item.Pipeline);

                list.SetGraphicsResourceSet(0, item.Resources);

                if (mat != null)
                    for (int i = 0; i < mat.Resources.Count; i++) list.SetGraphicsResourceSet((uint)i + 1, mat.Resources[i]);

                list.SetVertexBuffer(0, model.VertexBuffer);
                list.SetIndexBuffer(model.IndexBuffer, IndexFormat.UInt32);

                list.DrawIndexed(model.IndexCount);
            }
        }

        public void Draw(CommandList list, params GameObject[] obj)
        {
            foreach (var pass in Passes)
            {
                if (PrevSwapchain != pass.swapchain.Framebuffer)
                {
                    list.SetFramebuffer(pass.swapchain.Framebuffer);
                    PrevSwapchain = pass.swapchain.Framebuffer;
                }

                list.SetPipeline(pass.Pipeline);
                list.SetGraphicsResourceSet(0, pass.Resources);

                KumaMaterial lastBoundMaterial = null!;
                DeviceBuffer lastBoundVertexBuffer = null!;

                var sortedObjects = obj
                    .OrderBy(x => x.Material?.GetHashCode() ?? 0)
                    .ThenBy(x => x.Model.VertexBuffer.GetHashCode());

                foreach (var gameObject in sortedObjects)
                {
                    list.UpdateBuffer(ModelBuffer, 0, new RocketModelScheme(gameObject.Transform));

                    if (gameObject.Material != null && gameObject.Material != lastBoundMaterial)
                    {
                        for (int i = 0; i < gameObject.Material.Resources.Count; i++)
                        {
                            list.SetGraphicsResourceSet((uint)i + 1, gameObject.Material.Resources[i]);
                        }
                        lastBoundMaterial = gameObject.Material;
                    }

                    var model = gameObject.Model;
                    if (model.VertexBuffer != lastBoundVertexBuffer)
                    {
                        list.SetVertexBuffer(0, model.VertexBuffer);
                        list.SetIndexBuffer(model.IndexBuffer, IndexFormat.UInt32);
                        lastBoundVertexBuffer = model.VertexBuffer;
                    }

                    list.DrawIndexed(model.IndexCount);
                }
            }
        }
    }

    public class KumaPass
    {
        GraphicsPipelineDescription Descriptor;
        public Pipeline Pipeline;

        public List<TextureView> Textures { get; set; } = new();
        public ResourceSet Resources { get; set; } = null!;

        public static Dictionary<string, KumaSwapchain> SwapChains = new();

        public KumaSwapchain swapchain;

        public KumaPass(ResourceFactory factory,GraphicsPipelineDescription pipelineDescription) 
        {
            Descriptor = pipelineDescription;
            Pipeline = factory.CreateGraphicsPipeline(ref Descriptor);
        }

        public static KumaPass FromFile(ResourceFactory factory,string set,PassFile result,KumaPipeline pipeline, List<VertexElementDescription> layoutElements)
        {
            List<ResourceLayoutElementDescription> resourceLayoutElementDescriptions = new();
            List<ResourceLayoutElementDescription> textureLayoutElementDescriptions = new();

            foreach (var item in result.Uniforms)
            {
                switch (item.Value)
                {
                    case RoketPipelineUniforms.CameraProjView:
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

            if (!SwapChains.ContainsKey(result.Output))
                SwapChains.Add(result.Output, new(factory, result.Output + ".json"));

            var ret = new KumaPass(factory, new()
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
                    shaders: Shaders.FromSPIRVVertFrag(factory, set, result.VertexShader, result.FragmentShader)
                ),
                Outputs = SwapChains[result.Output].Framebuffer.OutputDescription
            });

            ret.swapchain = SwapChains[result.Output];

            List<BindableResource> bindableResources = new();

            foreach (var item in result.Uniforms)
            {
                switch (item.Value)
                {
                    case RoketPipelineUniforms.CameraProjView:
                        bindableResources.Add(Camera._cameraProjViewBuffer);
                        break;
                    case RoketPipelineUniforms.ObjectModelMatrix:
                        bindableResources.Add(pipeline.ModelBuffer);
                        break;
                    case RoketPipelineUniforms.Lights:
                        bindableResources.Add(KumaScene.PointLightBuffer);
                        break;
                    case RoketPipelineUniforms.CameraPos:
                        bindableResources.Add(Camera._cameraPosBuffer);
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
        public static void SetPass(this CommandList list, KumaPass pipeline) => list.SetPipeline(pipeline.Pipeline);

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
        CameraProjView,
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
        public List<PassFile> Passes = new();
        public Dictionary<string, RoketVertexElement> VertexDefinition = new();
    }

    public class PassFile
    {
        public string Output = "MainSwapchain";
        public string VertexShader = "";
        public string FragmentShader = "";
        public string ComputeShader = "";

        public Dictionary<string, RoketPipelineUniforms> Uniforms = new();
    }
}
