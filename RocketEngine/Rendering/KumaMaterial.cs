using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Veldrid;
using Vulkan;

namespace KumaEngine.Rendering
{
    public class KumaMaterial : IDisposable
    {
        public Guid MaterialID { get; } = new();

        public Dictionary<string, MaterialResource> Textures = new();

        Dictionary<string, CompiledMaterialResource> _resourceCache = new();

        Dictionary<KumaPass, List<ResourceSet>> _passSetCache = new();

        public void Invalidate()
        {
            _resourceCache.Clear();

            foreach (var item in Textures)
            {
                var resources = new List<BindableResource>();
                var set = new List<ResourceLayoutElementDescription>();

                switch (item.Value.Type)
                {
                    case KumaPipelineUniforms.SamplerCube:
                    case KumaPipelineUniforms.Sampler2D:
                    case KumaPipelineUniforms.Sampler3D:
                        set.AddRange(
                            new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                            new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
                        );
                        break;
                    default:
                        break;
                }

                switch (item.Value.Type)
                {
                    case KumaPipelineUniforms.SamplerCube:
                        resources.Add(
                            TextureExt.CubemapFromFile(
                                DefinitionFile.Game.GraphicsDevice, 
                                DefinitionFile.Game.ResourceFactory, 
                                item.Value.File, item.Value.Format
                            )
                        );
                        break;
                    case KumaPipelineUniforms.Sampler2D:
                        resources.Add(
                            TextureExt.ViewFromFile(
                                DefinitionFile.Game.GraphicsDevice, 
                                DefinitionFile.Game.ResourceFactory, 
                                item.Value.File, item.Value.Format
                            )
                        );
                        break;
                    case KumaPipelineUniforms.Sampler3D:
                        break;
                    default:
                        break;
                }

                switch (item.Value.Mode)
                {
                    case KumaSamplerMode.Linear:
                        resources.Add(DefinitionFile.Game.GraphicsDevice.LinearSampler);
                        break;
                    case KumaSamplerMode.Point:
                        resources.Add(DefinitionFile.Game.GraphicsDevice.PointSampler);
                        break;
                    case KumaSamplerMode.Ansio:
                        resources.Add(DefinitionFile.Game.GraphicsDevice.Aniso4xSampler);
                        break;
                    default:
                        break;
                }

                _resourceCache.Add(item.Key,new()
                {
                    Resources = resources,
                    Set = set
                });
            }

            _passSetCache.Clear();
        }

        public List<ResourceSet> GetCompiledMaterial(GraphicsDevice gd, ResourceFactory factory, KumaPass pass)
        {
            if (_passSetCache.TryGetValue(pass, out var sc))
                return sc;

            var set = new List<ResourceSet>();

            var descriptions = new List<ResourceLayoutElementDescription>();
            var bindings = new List<BindableResource>();

            foreach (var item in pass.definition.Samplers)
            {
                if (_resourceCache.TryGetValue(item.Key,out var resource))
                {
                    descriptions.AddRange(resource.Set);
                    bindings.AddRange(resource.Resources);
                }
                else
                {
                    descriptions.AddRange([
                        new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                        new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
                    ]);

                    bindings.AddRange([
                        TextureExt.GetNullTexture(gd,factory),
                        gd.PointSampler
                    ]);
                }
            }

            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription(descriptions.ToArray());
            ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(textureLayout, bindings.ToArray());
            var _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            set.Add(_sharedResourceSet);

            return set;
        }

        public void AddTextureSampler(ResourceFactory factory,string name,TextureView view,Sampler sampler)
        {
            var descriptions = new List<ResourceLayoutElementDescription>()
            {
                new ResourceLayoutElementDescription(name + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                new ResourceLayoutElementDescription(name + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
            };

            _resourceCache.Add(name,new() 
            {
                Set = descriptions,
                Resources = [view, sampler]
            });
        }

        public static KumaMaterial FromFile(GraphicsDevice device, ResourceFactory factory,string set)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = [ new StringEnumConverter() ]
            };

            var defpath = AssetRetriver.FetchAssetPath(AssetKind.Materials, set + ".json");

            if (!File.Exists(defpath)) throw new Exception("Could not find material definition file");

            var result = JsonConvert.DeserializeObject<MaterialFile>(File.ReadAllText(defpath), settings);

            KumaMaterial material = new();

            material.Textures = result.Textures;
            material.Invalidate();

            return material;
        }

        public void Dispose()
        {
            _passSetCache.Clear();
            _resourceCache.Clear();
        }
    }

    public class MaterialFile
    {
        public Dictionary<string, MaterialResource> Textures = new();
    }

    public class MaterialResource
    {
        public string File { get; set; } = "";
        public KumaPipelineUniforms Type { get; set; }
        public KumaSamplerMode Mode { get; set; } = KumaSamplerMode.Ansio;
        public PixelFormat Format { get; set; } = PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
    }

    public class CompiledMaterialResource
    {
        public List<BindableResource> Resources { get; set; } = [];
        public List<ResourceLayoutElementDescription> Set { get; set; } = [];
    }

    public enum KumaSamplerMode
    {
        Linear,
        Point,
        Ansio
    }
}
