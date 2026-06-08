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
        public List<ResourceSet> Resources { get; set; } = new();

        public void AddTextureSampler(ResourceFactory factory,string name,TextureView view,Sampler sampler)
        {
            ResourceLayoutDescription resourceLayoutDescription1 = new ResourceLayoutDescription([
                new ResourceLayoutElementDescription(name + "Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription(name + "Samp", ResourceKind.Sampler, ShaderStages.Fragment)
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

            var defpath = Path.Combine("Data","Materials", set + ".json");

            if (!File.Exists(defpath)) throw new Exception("Could not find material definition file");

            var result = JsonConvert.DeserializeObject<MaterialFile>(File.ReadAllText(defpath), settings);

            KumaMaterial material = new();

            List<ResourceLayoutElementDescription> textureLayoutElementDescriptions = new();

            foreach (var item in result.Textures)
            {
                switch (item.Value)
                {
                    case KumaPipelineUniforms.LinearSamplerCube:
                    case KumaPipelineUniforms.LinearSampler2D:
                    case KumaPipelineUniforms.LinearSampler3D:
                        textureLayoutElementDescriptions.AddRange(
                            new ResourceLayoutElementDescription(item.Key + "Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                            new ResourceLayoutElementDescription(item.Key + "Samp", ResourceKind.Sampler, ShaderStages.Fragment)
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
                switch (item.Value)
                {
                    case KumaPipelineUniforms.LinearSamplerCube:
                        bindableResources.Add(TextureExt.CubemapFromFile(device, factory, item.Key));
                        bindableResources.Add(device.Aniso4xSampler);
                        break;
                    case KumaPipelineUniforms.LinearSampler2D:
                        bindableResources.Add(TextureExt.ViewFromFile(device, factory, item.Key));
                        bindableResources.Add(device.Aniso4xSampler);
                        break;
                    case KumaPipelineUniforms.LinearSampler3D:
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
        public Dictionary<string, KumaPipelineUniforms> Textures = new();
    }
}
