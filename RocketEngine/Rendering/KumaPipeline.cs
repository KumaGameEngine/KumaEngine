using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpGen.Runtime;
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
using Vortice.Direct3D11;
using Vulkan;
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
        public Dictionary<string, KumaVertexElement> VertexDefinition { get; set; } = new();

        static KumaSwapchain PrevSwapchain = null!;

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
            PrevSwapchain = null!;

            foreach (var item in Passes)
            {
                if (item.IsCompute)
                {
                    list.SetPipeline(item.Pipeline);
                    list.SetComputeResourceSet(0, item.Resources);

                    if (mat != null)
                    {
                        for (int i = 0; i < mat.Resources.Count; i++)
                        {
                            list.SetComputeResourceSet((uint)i + 1, mat.Resources[i]);
                        }
                    }

                    if (item.definition.AutoGroups)
                    {
                        uint groupsX = (uint)((DefinitionFile.Game.Window.Width + item.definition.ThreadGroupSizeX - 1) / item.definition.ThreadGroupSizeX);
                        uint groupsY = (uint)((DefinitionFile.Game.Window.Height + item.definition.ThreadGroupSizeY - 1) / item.definition.ThreadGroupSizeY);

                        list.Dispatch(groupsX, groupsY, 1);
                    }
                    else list.Dispatch(
                        item.definition.GroupCountX, 
                        item.definition.GroupCountY, 
                        item.definition.GroupCountZ
                    );

                    continue;
                }

                if (PrevSwapchain != item.swapchain)
                {
                    list.SetFramebuffer(item.swapchain?.Framebuffer ?? DefinitionFile.Game.MainSwapchain.Framebuffer);
                    PrevSwapchain = item.swapchain ?? null!;
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
            PrevSwapchain = null!;

            foreach (var pass in Passes)
            {
                if (pass.IsCompute)
                {
                    list.SetPipeline(pass.Pipeline);
                    list.SetComputeResourceSet(0, pass.Resources);

                    KumaMaterial lastBoundMaterial = null!;
                    var sortedObjects = obj.OrderBy(x => x.Material?.GetHashCode() ?? 0);

                    foreach (var gameObject in sortedObjects)
                    {
                        list.UpdateBuffer(ModelBuffer, 0, new RocketModelScheme(gameObject.Transform));

                        if (gameObject.Material != null && gameObject.Material != lastBoundMaterial)
                        {
                            for (int i = 0; i < gameObject.Material.Resources.Count; i++)
                            {
                                list.SetComputeResourceSet((uint)i + 1, gameObject.Material.Resources[i]);
                            }
                            lastBoundMaterial = gameObject.Material;
                        }

                        if (pass.definition.AutoGroups)
                        {
                            uint groupsX = (uint)((DefinitionFile.Game.Window.Width + pass.definition.ThreadGroupSizeX - 1) / pass.definition.ThreadGroupSizeX);
                            uint groupsY = (uint)((DefinitionFile.Game.Window.Height + pass.definition.ThreadGroupSizeY - 1) / pass.definition.ThreadGroupSizeY);

                            list.Dispatch(groupsX, groupsY, 1);
                        }
                        else list.Dispatch(
                            pass.definition.GroupCountX,
                            pass.definition.GroupCountY,
                            pass.definition.GroupCountZ
                        );
                    }
                    continue;
                }

                if (PrevSwapchain != pass.swapchain)
                {
                    list.SetFramebuffer(pass.swapchain?.Framebuffer ?? DefinitionFile.Game.MainSwapchain.Framebuffer);
                    PrevSwapchain = pass.swapchain ?? null!;
                }

                list.SetPipeline(pass.Pipeline);
                list.SetGraphicsResourceSet(0, pass.Resources);

                KumaMaterial lastBoundGraphicsMaterial = null!;
                DeviceBuffer lastBoundVertexBuffer = null!;

                var sortedGraphicsObjects = obj
                    .OrderBy(x => x.Material?.GetHashCode() ?? 0)
                    .ThenBy(x => x.Model.VertexBuffer.GetHashCode());

                foreach (var gameObject in sortedGraphicsObjects)
                {
                    list.UpdateBuffer(ModelBuffer, 0, new RocketModelScheme(gameObject.Transform));

                    if (gameObject.Material != null && gameObject.Material != lastBoundGraphicsMaterial)
                    {
                        for (int i = 0; i < gameObject.Material.Resources.Count; i++)
                        {
                            list.SetGraphicsResourceSet((uint)i + 1, gameObject.Material.Resources[i]);
                        }
                        lastBoundGraphicsMaterial = gameObject.Material;
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
        ComputePipelineDescription ComputeDescriptor;
        public Pipeline Pipeline;
        public bool IsCompute { get; } = false;

        public PassFile definition { get; private set; }

        public List<(string name,bool access, DeviceBuffer buffer, uint size)> Buffers { get; private set; } = new();
        public ResourceSet Resources { get; set; } = null!;

        public static Dictionary<string, KumaSwapchain> SwapChains = new();

        public KumaSwapchain swapchain;

        public KumaPass(ResourceFactory factory,GraphicsPipelineDescription pipelineDescription) 
        {
            Descriptor = pipelineDescription;
            Pipeline = factory.CreateGraphicsPipeline(ref Descriptor);
        }

        public KumaPass(ResourceFactory factory, ComputePipelineDescription pipelineDescription)
        {
            ComputeDescriptor = pipelineDescription;
            Pipeline = factory.CreateComputePipeline(ref ComputeDescriptor);

            IsCompute = true;
        }

        public static KumaPass FromFile(ResourceFactory factory,string set,PassFile result,KumaPipeline pipeline, List<VertexElementDescription> layoutElements)
        {
            List<ResourceLayoutElementDescription> staticResourceLayoutElementDescriptions = new();
            List<ResourceLayoutElementDescription> dynamicResourceLayoutElementDescriptions = new();

            foreach (var item in result.Uniforms)
            {
                var enumValues = KumaPipelineUniforms.NULL;

                if (Enum.TryParse<KumaPipelineUniforms>(item.Value,true, out var parsedEnum))
                    enumValues = parsedEnum;

                switch (enumValues)
                {
                    case KumaPipelineUniforms.CameraProjView:
                    case KumaPipelineUniforms.CameraProjViewInverse:
                    case KumaPipelineUniforms.ObjectModelMatrix:
                    case KumaPipelineUniforms.Lights:
                    case KumaPipelineUniforms.CameraPos:
                        staticResourceLayoutElementDescriptions.Add(
                            new ResourceLayoutElementDescription(item.Key, ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
                        );
                        break;
                    case KumaPipelineUniforms.LinearSamplerCube:
                    case KumaPipelineUniforms.LinearSampler2D:
                    case KumaPipelineUniforms.LinearSampler3D:
                        dynamicResourceLayoutElementDescriptions.AddRange(
                            new ResourceLayoutElementDescription(item.Key+"Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                            new ResourceLayoutElementDescription(item.Key+"Samp", ResourceKind.Sampler, ShaderStages.Fragment)
                        );
                        break;
                    case KumaPipelineUniforms.NULL:
                        if (SwapChains.Any(x => x.Value != null && x.Value.Attachments.ContainsKey(item.Value)))
                            staticResourceLayoutElementDescriptions.AddRange(
                                new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                                new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, ShaderStages.Fragment)
                            );
                        else
                        {
                            var (name, access, size) = ParseBuffer(item.Value);

                            staticResourceLayoutElementDescriptions.AddRange(
                                new ResourceLayoutElementDescription(
                                    name, 
                                    access ? ResourceKind.StructuredBufferReadWrite : ResourceKind.StructuredBufferReadOnly,
                                    ShaderStages.Vertex | ShaderStages.Fragment)
                            );
                        }
                        break;
                    default:
                        break;
                }
            }

            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(staticResourceLayoutElementDescriptions.ToArray());
            ResourceLayout sharedLayout = factory.CreateResourceLayout(resourceLayoutDescription);

            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription(dynamicResourceLayoutElementDescriptions.ToArray());
            ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

            if (!SwapChains.ContainsKey(result.Output))
                SwapChains.Add(result.Output, new(factory, result.Output + ".json"));

            var ret = result.Type switch
            {
                KumaShaderType.Graphics => GetGraphicsPass(factory, result, set, new[] { sharedLayout, textureLayout }, layoutElements),
                KumaShaderType.Compute => GetComputePass(factory, result, set, new[] { sharedLayout, textureLayout }),
                _ => throw new Exception("Invalid shader type in pipeline definition")
            };

            ret.definition = result;

            ret.swapchain = SwapChains[result.Output];

            List<BindableResource> bindableResources = new();

            foreach (var item in result.Uniforms)
            {
                var enumValues = KumaPipelineUniforms.NULL;

                if (Enum.TryParse<KumaPipelineUniforms>(item.Value,true, out var parsedEnum))
                    enumValues = parsedEnum;

                switch (enumValues)
                {
                    case KumaPipelineUniforms.CameraProjView:
                        bindableResources.Add(Camera._cameraProjViewBuffer);
                        break;
                    case KumaPipelineUniforms.CameraProjViewInverse:
                        bindableResources.Add(Camera._cameraProjViewInverseBuffer);
                        break;
                    case KumaPipelineUniforms.ObjectModelMatrix:
                        bindableResources.Add(pipeline.ModelBuffer);
                        break;
                    case KumaPipelineUniforms.Lights:
                        bindableResources.Add(KumaScene.PointLightBuffer);
                        break;
                    case KumaPipelineUniforms.CameraPos:
                        bindableResources.Add(Camera._cameraPosBuffer);
                        break;
                    case KumaPipelineUniforms.NULL:
                        var tex = SwapChains.FirstOrDefault(x => x.Value != null && x.Value.Attachments.ContainsKey(item.Value));

                        if(tex.Value != null)
                        {
                            bindableResources.Add(tex.Value.Attachments[item.Value]);
                            bindableResources.Add(DefinitionFile.Game.GraphicsDevice.Aniso4xSampler);

                            continue;
                        }

                        var (name, access, size) = ParseBuffer(item.Value);

                        if (pipeline.Passes.Any(x => x.Buffers.Any(b => b.name == name)))
                        {
                            var existingBuffer = pipeline.Passes.SelectMany(x => x.Buffers).First(b => b.name == name);

                            if (existingBuffer.access != access || existingBuffer.size != size)
                                throw new Exception($"Buffer \"{name}\" has conflicting declarations in the pipeline definition");

                            bindableResources.Add(existingBuffer.buffer);
                            continue;
                        }

                        var buffer = factory.CreateBuffer(
                            new(
                                size,
                                access ? BufferUsage.StructuredBufferReadWrite : BufferUsage.StructuredBufferReadOnly
                            )
                        );

                        ret.Buffers.Add((name, access, buffer, size));
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

        static (string,bool,uint) ParseBuffer(string Value)
        {
            var parts = Value.Split(';');

            if (parts.Length != 3)
                throw new Exception(
                    $"Invalid buffer declaration: \"{Value}\", " +
                    $"must be: \"<name>;<access>;<size>\""
                );

            if (new[] { "ro", "rw" }.Contains(parts[1].Trim().ToLower()))
                throw new Exception(
                    $"Invalid buffer access type: \"{parts[1]}\", " +
                    $"must be either \"ro\" or \"rw\""
                );

            var name = parts[0].Trim();
            bool access = parts[1].Trim().ToLower() == "rw";
            uint size = uint.Parse(parts[2].Trim(), System.Globalization.NumberStyles.Any);

            return (name, access, size);
        }

        static KumaPass GetGraphicsPass(
            ResourceFactory factory,
            PassFile result, 
            string set, 
            ResourceLayout[] layouts, 
            List<VertexElementDescription> layoutElements
        )
        {
            return new KumaPass(factory, new GraphicsPipelineDescription()
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
                ResourceLayouts = layouts,
                ShaderSet = new ShaderSetDescription(
                    vertexLayouts: [new(layoutElements.ToArray())],
                    shaders: Shaders.FromSPIRVVertFrag(factory, set, result.VertexShader, result.FragmentShader)
                ),
                Outputs = SwapChains[result.Output]?.Framebuffer.OutputDescription ?? DefinitionFile.Game.MainSwapchain.Framebuffer.OutputDescription
            });
        }

        static KumaPass GetComputePass(
            ResourceFactory factory,
            PassFile result,
            string set,
            ResourceLayout[] layouts
        )
        {
            return new KumaPass(factory, new ComputePipelineDescription()
            {
                ComputeShader = Shaders.FromSPIRVCompute(factory, set, result.ComputeShader),
                ResourceLayouts = layouts,
                ThreadGroupSizeX = result.ThreadGroupSizeX,
                ThreadGroupSizeY = result.ThreadGroupSizeY,
                ThreadGroupSizeZ = result.ThreadGroupSizeZ,
            });
        }
    }

    public static class RocketCommandListExtensions
    {
        public static void SetPass(this CommandList list, KumaPass pipeline) => list.SetPipeline(pipeline.Pipeline);

        public static VertexElementFormat ToFormat(this KumaVertexElement element) => element switch
        {
            KumaVertexElement.UV => VertexElementFormat.Float2,
            KumaVertexElement.UVW => VertexElementFormat.Float3,
            KumaVertexElement.Position => VertexElementFormat.Float3,
            KumaVertexElement.Normal => VertexElementFormat.Float3,
            KumaVertexElement.Color => VertexElementFormat.Float4,
            KumaVertexElement.Tangent => VertexElementFormat.Float3,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public enum KumaPipelineUniforms
    {
        CameraProjView,
        CameraProjViewInverse,
        CameraPos,
        ObjectModelMatrix,
        LinearSamplerCube,
        LinearSampler2D,
        LinearSampler3D,
        Lights,
        NULL
    }
    public enum KumaVertexElement
    {
        UV,
        UVW,
        Position,
        Normal,
        Color,
        Tangent,
    }

    public enum KumaShaderType
    {
        Graphics,
        Compute
    }

    public class PipelineFile
    {
        public List<PassFile> Passes = new();
        public Dictionary<string, KumaVertexElement> VertexDefinition = new();
    }

    public class PassFile
    {
        public string Output = "MainSwapchain";
        public string VertexShader = "";
        public string FragmentShader = "";
        public string ComputeShader = "";
        public KumaShaderType Type = KumaShaderType.Graphics;
        public uint ThreadGroupSizeX = 1;
        public uint ThreadGroupSizeY = 1;
        public uint ThreadGroupSizeZ = 1;
        public uint GroupCountX = 1;
        public uint GroupCountY = 1;
        public uint GroupCountZ = 1;
        public bool AutoGroups = false;

        public Dictionary<string, string> Uniforms = new();
    }
}
