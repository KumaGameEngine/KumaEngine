using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using KumaEngine.API;
using KumaEngine.Rendering.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class KumaMaterial : IDisposable
    {
        public Guid MaterialID { get; } = new();
        public List<ResourceSet> Resources { get; set; } = new();

        public void AddTextureSampler(ResourceFactory factory,string name,TextureView view,Sampler sampler)
        {
            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription([
                new ResourceLayoutElementDescription(name + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                new ResourceLayoutElementDescription(name + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
            ]);
            ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(textureLayout, [
                view, sampler
            ]);
            var _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            Resources.Add(_sharedResourceSet);
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

            List<ResourceLayoutElementDescription> textureLayoutElementDescriptions = new();

            foreach (var item in result.Textures)
            {
                switch (item.Value.Type)
                {
                    case KumaPipelineUniforms.SamplerCube:
                    case KumaPipelineUniforms.Sampler2D:
                    case KumaPipelineUniforms.Sampler3D:
                        textureLayoutElementDescriptions.AddRange(
                            new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, KumaPipeline.STAGE_GLOBAL),
                            new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, KumaPipeline.STAGE_GLOBAL)
                        );
                        break;
                    default:
                        break;
                }
            }

            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription(textureLayoutElementDescriptions.ToArray());
            ResourceLayout textureLayout = factory.CreateResourceLayout(resourceLayoutDescription1);

            List<BindableResource> bindableResources = new();

            foreach (var item in result.Textures)
            {
                switch (item.Value.Type)
                {
                    case KumaPipelineUniforms.SamplerCube:
                        bindableResources.Add(TextureExt.CubemapFromFile(device, factory, item.Value.File, item.Value.Format));
                        break;
                    case KumaPipelineUniforms.Sampler2D:
                        bindableResources.Add(TextureExt.ViewFromFile(device, factory, item.Value.File, item.Value.Format));
                        break;
                    case KumaPipelineUniforms.Sampler3D:
                        break;
                    default:
                        break;
                }

                switch (item.Value.Mode)
                {
                    case KumaSamplerMode.Linear:
                        bindableResources.Add(device.LinearSampler);
                        break;
                    case KumaSamplerMode.Point:
                        bindableResources.Add(device.PointSampler);
                        break;
                    case KumaSamplerMode.Ansio:
                        bindableResources.Add(device.Aniso4xSampler);
                        break;
                    default:
                        break;
                }
            }

            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(textureLayout, bindableResources.ToArray());
            var _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            material.Resources.Add(_sharedResourceSet);

            return material;
        }

        public void Dispose()
        {
            foreach (var item in Resources) item.Dispose();
        }
    }

    public class MaterialFile
    {
        public Dictionary<string, MaterialResource> Textures = new();
    }

    public class MaterialResource
    {
        public string File { get; set; }
        public KumaPipelineUniforms Type { get; set; }
        public KumaSamplerMode Mode { get; set; } = KumaSamplerMode.Ansio;
        public PixelFormat Format { get; set; } = PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
    }

    public enum KumaSamplerMode
    {
        Linear,
        Point,
        Ansio
    }
}
