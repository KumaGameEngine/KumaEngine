using Assimp;
using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
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
        public List<(string Key, KumaVertexElement Value)> VertexDefinition { get; set; } = new();

        static bool GraphicsDispatchable = false;

        static Framebuffer _previousFB = null!;

        public const ShaderStages STAGE_GLOBAL = ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute;

        public KumaPipeline(ResourceFactory factory)
        {
            ModelBuffer = factory.CreateBuffer(new(RocketModelScheme.Size, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        }

        public static void ResetFramebuffer() => _previousFB = null!;

        public static KumaPipeline FromSet(ResourceFactory factory, string set)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = [new StringEnumConverter()]
            };

            var defpath = AssetRetriver.FetchAssetPath(AssetKind.Shaders, Path.Combine(set, "pipeline.json"));

            if (!File.Exists(defpath)) throw new Exception("Could not find pipeline definition file");

            var result = JsonConvert.DeserializeObject<PipelineFile>(File.ReadAllText(defpath), settings);

            List<VertexElementDescription> layoutElements = new();
            foreach (var item in result.VertexDefinition)
                layoutElements.Add(new(item.Key, item.Value.ToFormat(), VertexElementSemantic.TextureCoordinate));

            var pipeline = new KumaPipeline(factory);

            pipeline.VertexDefinition = result.VertexDefinition;

            foreach (var item in result.Passes)
                pipeline.Passes.Add(KumaPass.FromFile(factory, set, item, pipeline, layoutElements));

            return pipeline;
        }

        static (uint x, uint y, uint z) ComputeGroupCounts(PassFile definition)
        {
            if (definition.AutoGroups)
            {
                uint groupsX = (DefinitionFile.Game.Window.Width + definition.ThreadGroupSizeX - 1) / definition.ThreadGroupSizeX;
                uint groupsY = (DefinitionFile.Game.Window.Height + definition.ThreadGroupSizeY - 1) / definition.ThreadGroupSizeY;
                return (groupsX, groupsY, 1u);
            }

            return (definition.GroupCountX, definition.GroupCountY, definition.GroupCountZ);
        }

        public void Draw(CommandList list, Model model, KumaMaterial mat)
        {
            var mdl = model.GetCompiledVerticies(
                DefinitionFile.Game.GraphicsDevice,
                DefinitionFile.Game.GraphicsDevice.ResourceFactory,
                this
            );

            foreach (var item in Passes)
            {
                if (item.IsCompute)
                {
                    var (groupsX, groupsY, groupsZ) = ComputeGroupCounts(item.definition);

                    item.EnsureAutoBufferSize(groupsX * groupsY * groupsZ);

                    list.SetPipeline(item.Pipeline);
                    list.SetComputeResourceSet(0, item.Resources);

                    if (mat != null)
                    {
                        var resources = mat.GetCompiledMaterial(
                            DefinitionFile.Game.GraphicsDevice, 
                            DefinitionFile.Game.ResourceFactory,
                            item
                        );

                        for (int i = 0; i < resources.Count; i++)
                            list.SetComputeResourceSet((uint)i + 1, resources[i]);
                    }

                    list.Dispatch(groupsX, groupsY, groupsZ);

                    continue;
                }

                if (_previousFB != item.swapchain?.Framebuffer)
                {
                    list.SetFramebuffer(item.swapchain?.Framebuffer ?? DefinitionFile.Game.MainSwapchain.Framebuffer);
                    _previousFB = item.swapchain?.Framebuffer!;
                }

                list.SetPipeline(item.Pipeline);
                list.SetGraphicsResourceSet(0, item.Resources);

                if (item.definition.UseMaterial && mat != null)
                {
                    var resources = mat.GetCompiledMaterial(
                        DefinitionFile.Game.GraphicsDevice,
                        DefinitionFile.Game.ResourceFactory,
                        item
                    );

                    for (int i = 0; i < resources.Count; i++)
                        list.SetGraphicsResourceSet((uint)i + 1, resources[i]);
                }

                list.SetVertexBuffer(0, mdl);
                list.SetIndexBuffer(model.IndexBuffer, IndexFormat.UInt32);

                list.DrawIndexed((uint)model.Indices.Count);
            }
        }

        public void Draw(CommandList GraphicsList, CommandList ComputeList, params GameObject[] obj)
        {
            bool lastPassCompute = false;

            foreach (var pass in Passes)
            {
                if (lastPassCompute != pass.IsCompute)
                {
                    if (pass.IsCompute && GraphicsDispatchable)
                    {
                        GraphicsList.End();

                        DefinitionFile.Game.GraphicsDevice.SubmitCommands(GraphicsList, DefinitionFile.Game.GraphicsFence);
                        DefinitionFile.Game.GraphicsDevice.WaitForFence(DefinitionFile.Game.GraphicsFence, 5000000000);
                        DefinitionFile.Game.GraphicsDevice.ResetFence(DefinitionFile.Game.GraphicsFence);

                        GraphicsList.Begin();

                        GraphicsDispatchable = false;
                    }
                    else
                    {
                        ComputeList.End();

                        DefinitionFile.Game.GraphicsDevice.SubmitCommands(ComputeList, DefinitionFile.Game.ComputeFence);
                        DefinitionFile.Game.GraphicsDevice.WaitForFence(DefinitionFile.Game.ComputeFence, 5000000000);
                        DefinitionFile.Game.GraphicsDevice.ResetFence(DefinitionFile.Game.ComputeFence);

                        ComputeList.Begin();
                    }

                    lastPassCompute = pass.IsCompute;
                }

                if (pass.IsCompute)
                {
                    var (groupsX, groupsY, groupsZ) = ComputeGroupCounts(pass.definition);

                    pass.EnsureAutoBufferSize(groupsX * groupsY * groupsZ);

                    ComputeList.SetPipeline(pass.Pipeline);
                    ComputeList.SetComputeResourceSet(0, pass.Resources);

                    KumaMaterial lastBoundMaterial = null!;
                    var sortedObjects = obj.OrderBy(x => x.Material?.GetHashCode() ?? 0);

                    void Dispatch() => ComputeList.Dispatch(groupsX, groupsY, groupsZ);

                    if (pass.definition.ComputePerObject) foreach (var gameObject in sortedObjects)
                    {
                        ComputeList.UpdateBuffer(ModelBuffer, 0, new RocketModelScheme(gameObject.Transform));

                        if (pass.definition.UseMaterial && gameObject.Material != null && gameObject.Material != lastBoundMaterial)
                        {
                            var resources = gameObject.Material.GetCompiledMaterial(
                                DefinitionFile.Game.GraphicsDevice,
                                DefinitionFile.Game.ResourceFactory,
                                pass
                            );

                            for (int i = 0; i < resources.Count; i++)
                                    ComputeList.SetComputeResourceSet((uint)i + 1, resources[i]);

                            lastBoundMaterial = gameObject.Material;
                        }

                        Dispatch();
                    }
                    else Dispatch();
                    continue;
                }

                if (_previousFB != pass.swapchain?.Framebuffer)
                {
                    GraphicsList.SetFramebuffer(pass.swapchain?.Framebuffer ?? DefinitionFile.Game.MainSwapchain.Framebuffer);
                    _previousFB = pass.swapchain?.Framebuffer!;
                }

                GraphicsList.SetPipeline(pass.Pipeline);
                GraphicsList.SetGraphicsResourceSet(0, pass.Resources);

                KumaMaterial lastBoundGraphicsMaterial = null!;
                DeviceBuffer lastBoundVertexBuffer = null!;

                var sortedGraphicsObjects = obj
                    .OrderBy(x => x.Material?.MaterialID ?? Guid.Empty)
                    .ThenBy(x => x.Model.ModelID);

                foreach (var gameObject in sortedGraphicsObjects)
                {
                    GraphicsList.UpdateBuffer(ModelBuffer, 0, new RocketModelScheme(gameObject.Transform));

                    if (pass.definition.UseMaterial && gameObject.Material != null && gameObject.Material != lastBoundGraphicsMaterial)
                    {
                        var resources = gameObject.Material.GetCompiledMaterial(
                            DefinitionFile.Game.GraphicsDevice,
                            DefinitionFile.Game.ResourceFactory,
                            pass
                        );

                        for (int i = 0; i < resources.Count; i++)
                            GraphicsList.SetGraphicsResourceSet((uint)i + 1, resources[i]);

                        lastBoundGraphicsMaterial = gameObject.Material;
                    }

                    var model = gameObject.Model;

                    var mdl = model.GetCompiledVerticies(
                        DefinitionFile.Game.GraphicsDevice,
                        DefinitionFile.Game.GraphicsDevice.ResourceFactory,
                        this
                    );

                    if (mdl != lastBoundVertexBuffer)
                    {
                        GraphicsList.SetVertexBuffer(0, mdl);
                        GraphicsList.SetIndexBuffer(model.IndexBuffer, IndexFormat.UInt32);
                        lastBoundVertexBuffer = mdl;
                    }

                    GraphicsList.DrawIndexed((uint)model.Indices.Count);
                }

                GraphicsDispatchable = true;
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

        public List<(string name, DeviceBuffer buffer)> ReadBuffer { get; private set; } = new();
        public List<(string name, bool access, DeviceBuffer buffer, uint size)> Buffers { get; private set; } = new();
        public ResourceSet Resources { get; set; } = null!;

        public static Dictionary<string, KumaSwapchain> SwapChains = new();

        public KumaSwapchain swapchain;

        public ResourceFactory Factory { get; internal set; } = null!;

        public ResourceLayout SharedLayout { get; internal set; } = null!;

        public List<BindableResource> BindableResources { get; internal set; } = new();

        public class AutoBufferSlot
        {
            public string Name = "";
            public bool ReadWrite;
            public uint Stride;
            public int BindableIndex;
            public uint LastGroupCount = uint.MaxValue;
        }

        public List<AutoBufferSlot> AutoBuffers { get; private set; } = new();

        public List<(KumaPass consumer, int bindableIndex)> Consumers { get; private set; } = new();

        public void EnsureAutoBufferSize(uint totalGroupCount)
        {
            if (AutoBuffers.Count == 0) return;

            foreach (var auto in AutoBuffers)
            {
                if (auto.LastGroupCount == totalGroupCount) continue;

                uint newSize = Math.Max(totalGroupCount, 1u) * auto.Stride;

                var newBuffer = Factory.CreateBuffer(new (
                    newSize,
                    auto.ReadWrite ? BufferUsage.StructuredBufferReadWrite : BufferUsage.StructuredBufferReadOnly,
                    auto.Stride
                ));

                int bufIdx = Buffers.FindIndex(b => b.name == auto.Name);
                if (bufIdx >= 0) Buffers[bufIdx] = (auto.Name, auto.ReadWrite, newBuffer, newSize);

                RebindResource(auto.BindableIndex, newBuffer);

                foreach (var (consumer, consumerIndex) in Consumers)
                    consumer.RebindResource(consumerIndex, newBuffer);

                auto.LastGroupCount = totalGroupCount;
            }
        }

        public void RebindResource(int bindableIndex, BindableResource newResource)
        {
            BindableResources[bindableIndex] = newResource;
            Resources = Factory.CreateResourceSet(new ResourceSetDescription(SharedLayout, BindableResources.ToArray()));
        }

        public KumaPass(ResourceFactory factory, GraphicsPipelineDescription pipelineDescription)
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

        public static KumaPass FromFile(ResourceFactory factory, string set, PassFile result, KumaPipeline pipeline, List<VertexElementDescription> layoutElements)
        {
            List<ResourceLayoutElementDescription> staticResourceLayoutElementDescriptions = new();
            List<ResourceLayoutElementDescription> dynamicResourceLayoutElementDescriptions = new();

            foreach (var item in result.Uniforms)
            {
                var enumValues = KumaPipelineUniforms.NULL;

                if (Enum.TryParse<KumaPipelineUniforms>(item.Value, true, out var parsedEnum))
                    enumValues = parsedEnum;

                switch (enumValues)
                {
                    case KumaPipelineUniforms.CameraProjView:
                    case KumaPipelineUniforms.CameraProjViewInverse:
                    case KumaPipelineUniforms.ObjectModelMatrix:
                    case KumaPipelineUniforms.Lights:
                    case KumaPipelineUniforms.CameraPos:
                        staticResourceLayoutElementDescriptions.Add(
                            new ResourceLayoutElementDescription(item.Key, ResourceKind.UniformBuffer, KumaPipeline.STAGE_GLOBAL)
                        );
                        break;
                    case KumaPipelineUniforms.NULL:
                        if (SwapChains.Any(x => x.Value != null && x.Value.Attachments.ContainsKey(item.Value)))
                            staticResourceLayoutElementDescriptions.AddRange(
                                new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                                new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
                            );
                        else
                        {
                            if (pipeline.Passes.Any(x => x.Buffers.Any(b => b.name == item.Value)))
                            {
                                var existingBuffer = pipeline.Passes.SelectMany(x => x.Buffers).First(b => b.name == item.Value);

                                staticResourceLayoutElementDescriptions.AddRange(
                                    new ResourceLayoutElementDescription(
                                        item.Key,
                                        ResourceKind.StructuredBufferReadOnly,
                                        KumaPipeline.STAGE_GLOBAL)
                                );

                                continue;
                            }

                            var (_, access, _, _, _) = ParseBuffer(item.Value, result);

                            if (result.Type != KumaShaderType.Compute)
                                throw new Exception("Structured buffers can only be created in compute shaders.");

                            staticResourceLayoutElementDescriptions.AddRange(
                                new ResourceLayoutElementDescription(
                                    item.Key,
                                    access ? ResourceKind.StructuredBufferReadWrite : ResourceKind.StructuredBufferReadOnly,
                                    ShaderStages.Compute
                                )
                            );
                        }
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in result.Samplers)
            {
                var enumValues = KumaPipelineUniforms.NULL;

                if (Enum.TryParse<KumaPipelineUniforms>(item.Value, true, out var parsedEnum))
                    enumValues = parsedEnum;

                switch (enumValues)
                {
                    case KumaPipelineUniforms.SamplerCube:
                    case KumaPipelineUniforms.Sampler2D:
                    case KumaPipelineUniforms.Sampler3D:
                        dynamicResourceLayoutElementDescriptions.AddRange(
                            new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                            new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
                        );
                        break;
                    default:
                        break;
                }
            }

            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(staticResourceLayoutElementDescriptions.ToArray());
            ResourceLayout sharedLayout = factory.CreateResourceLayout(resourceLayoutDescription);

            if (!SwapChains.ContainsKey(result.Output))
                SwapChains.Add(result.Output, new(factory, result.Output + ".json"));

            List<ResourceLayout> layouts = [sharedLayout];

            if (dynamicResourceLayoutElementDescriptions.Count > 0)
            {
                ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription(dynamicResourceLayoutElementDescriptions.ToArray());
                ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

                layouts.Add(textureLayout);
            }

            var ret = result.Type switch
            {
                KumaShaderType.Graphics => GetGraphicsPass(factory, result, set, layouts.ToArray(), layoutElements),
                KumaShaderType.Compute => GetComputePass(factory, result, set, layouts.ToArray()),
                _ => throw new Exception("Invalid shader type in pipeline definition")
            };

            ret.definition = result;

            if (result.Type != KumaShaderType.Compute)
                ret.swapchain = SwapChains[result.Output];

            ret.Factory = factory;
            ret.SharedLayout = sharedLayout;

            List<BindableResource> bindableResources = new();

            foreach (var item in result.Uniforms)
            {
                var enumValues = KumaPipelineUniforms.NULL;

                if (Enum.TryParse<KumaPipelineUniforms>(item.Value, true, out var parsedEnum))
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

                        if (tex.Value != null)
                        {
                            bindableResources.Add(tex.Value.Attachments[item.Value]);
                            bindableResources.Add(DefinitionFile.Game.GraphicsDevice.Aniso4xSampler);

                            continue;
                        }

                        if (pipeline.Passes.Any(x => x.Buffers.Any(b => b.name == item.Value)))
                        {
                            var producingPass = pipeline.Passes.First(x => x.Buffers.Any(b => b.name == item.Value));
                            var existingBuffer = producingPass.Buffers.First(b => b.name == item.Value);

                            ret.ReadBuffer.Add((item.Value, existingBuffer.buffer));

                            producingPass.Consumers.Add((ret, bindableResources.Count));

                            bindableResources.Add(existingBuffer.buffer);
                            continue;
                        }

                        var (name, access, isAuto, size, stride) = ParseBuffer(item.Value, result);

                        uint initialGroupCount = isAuto
                            ? (result.AutoGroups ? 1u : result.GroupCountX * result.GroupCountY * result.GroupCountZ)
                            : 0u;

                        uint bufSize = isAuto ? Math.Max(initialGroupCount, 1u) * stride : size;

                        var buffer = factory.CreateBuffer(
                            new(
                                bufSize,
                                access ? BufferUsage.StructuredBufferReadWrite : BufferUsage.StructuredBufferReadOnly,
                                stride
                            )
                        );

                        ret.Buffers.Add((name, access, buffer, bufSize));

                        if (isAuto)
                        {
                            ret.AutoBuffers.Add(new AutoBufferSlot
                            {
                                Name = name,
                                ReadWrite = access,
                                Stride = stride,
                                BindableIndex = bindableResources.Count,
                                LastGroupCount = initialGroupCount
                            });
                        }

                        bindableResources.Add(buffer);
                        break;
                    default:
                        break;
                }
            }

            ret.BindableResources = bindableResources;

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(sharedLayout, bindableResources.ToArray());
            var _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            ret.Resources = _sharedResourceSet;

            return ret;
        }

        static (string name, bool access, bool isAuto, uint size, uint stride) ParseBuffer(string Value, PassFile file)
        {
            var parts = Value.Split(';');

            if (parts.Length != 4 && parts.Length != 3)
                throw new Exception(
                    $"Invalid buffer declaration: \"{Value}\", " +
                    $"must be: \"<name>;<access>;<count>|auto;[stride]\""
                );

            if (!new[] { "ro", "rw" }.Contains(parts[1].Trim().ToLower()))
                throw new Exception(
                    $"Invalid buffer access type: \"{parts[1]}\", " +
                    $"must be either \"ro\" or \"rw\""
                );

            var name = parts[0].Trim();
            bool access = parts[1].Trim().ToLower() == "rw";
            var cstr = parts[2].Trim();
            bool isAuto = cstr.ToLower() == "auto";

            if (isAuto)
            {
                if (parts.Length != 4)
                    throw new Exception(
                        $"Auto-sized buffer \"{name}\" must declare an explicit stride: " +
                        $"\"{name};{(access ? "rw" : "ro")};auto;<stride>\""
                    );

                if (file.Type != KumaShaderType.Compute)
                    throw new Exception(
                        $"Auto-sized buffer \"{name}\" is only valid on a compute pass, " +
                        "since its size is derived from that pass's dispatch dimensions."
                    );

                uint autoStride = uint.Parse(parts[3].Trim(), System.Globalization.NumberStyles.Any);

                return (name, access, true, 0u, autoStride);
            }

            uint count = uint.Parse(cstr, System.Globalization.NumberStyles.Any);
            uint stride = parts.Length == 4 ? uint.Parse(parts[3].Trim(), System.Globalization.NumberStyles.Any) : count;
            uint size = count * (stride == count ? 1 : stride);

            return (name, access, false, size, stride);
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
                    depthTestEnabled: result.DepthTest,
                    depthWriteEnabled: result.DepthWrite,
                    comparisonKind: result.DepthComparison),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: result.CullMode,
                    fillMode: result.FillMode,
                    frontFace: result.FrontFace,
                    depthClipEnabled: result.DepthClipEnabled,
                    scissorTestEnabled: result.ScissorTestEnabled
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
        SamplerCube,
        Sampler2D,
        Sampler3D,
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

        [JsonConverter(typeof(OrderedKeyValueConverter<string, KumaVertexElement>))]
        public List<(string Key, KumaVertexElement Value)> VertexDefinition = new();
    }

    public class PassFile
    {
        public string Output = "MainSwapchain";
        public string VertexShader = null!;
        public string FragmentShader = null!;
        public string ComputeShader = null!;
        public KumaShaderType Type = KumaShaderType.Graphics;
        public uint ThreadGroupSizeX = 1;
        public uint ThreadGroupSizeY = 1;
        public uint ThreadGroupSizeZ = 1;
        public uint GroupCountX = 1;
        public uint GroupCountY = 1;
        public uint GroupCountZ = 1;
        public bool AutoGroups = false;
        public bool ComputePerObject = false;
        public bool UseMaterial = true;
        public bool DepthTest = true;
        public bool DepthWrite = true;
        public ComparisonKind DepthComparison = ComparisonKind.LessEqual;
        public FaceCullMode CullMode = FaceCullMode.Back;
        public PolygonFillMode FillMode = PolygonFillMode.Solid;
        public FrontFace FrontFace = FrontFace.CounterClockwise;
        public bool DepthClipEnabled = true;
        public bool ScissorTestEnabled = true;

        [JsonConverter(typeof(OrderedKeyValueConverter<string,string>))]
        public List<(string Key, string Value)> Uniforms = new();

        [JsonConverter(typeof(OrderedKeyValueConverter<string, string>))]
        public List<(string Key, string Value)> Samplers = new();
    }
}