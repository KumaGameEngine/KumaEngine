using Assimp;
using KumaEngine.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan;

namespace KumaEngine.Rendering
{
    public class KumaSwapchain
    {
        public Framebuffer Framebuffer;
        public Dictionary<string, TextureView> Attachments = new();

        public List<bool> ClearColorIDS = new();
        public List<bool> BlitColorIDS = new();

        public bool ClearDepth = true;
        public int BlitDepth = -1;

        List<Texture> _colorTextures = new();
        Texture _DepthTexture = null!;
        SwapchainFile _result;

        public bool SupportsResizing { get; } = true;

        public static KumaSwapchain FromVeldrid(ResourceFactory factory,Swapchain swapchain,string name)
        {
            var sw = new KumaSwapchain();
            sw.Framebuffer = swapchain.Framebuffer;

            if (swapchain.Framebuffer.DepthTarget.HasValue)
                sw._DepthTexture = swapchain.Framebuffer.DepthTarget.Value.Target;

            for (int i = 0; i < swapchain.Framebuffer.ColorTargets.Count; i++)
            {
                var item = swapchain.Framebuffer.ColorTargets[i];

                sw._colorTextures.Add(item.Target);
            }

            return sw;
        }

        public KumaSwapchain() 
        {
            SupportsResizing = false;
        }

        public KumaSwapchain(ResourceFactory factory, SwapchainFile file)
        {
            _result = file;
            Resize(factory);
        }

        public KumaSwapchain(ResourceFactory factory, string definitionFile)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = [new StringEnumConverter()]
            };

            var defpath = Path.Combine("Data", "Swapchains", definitionFile);

            if (!File.Exists(defpath)) throw new Exception("Could not find swapchain definition file");

            _result = JsonConvert.DeserializeObject<SwapchainFile>(File.ReadAllText(defpath), settings);
            Resize(factory);
        }

        public void Resize(ResourceFactory factory)
        {
            if (!SupportsResizing) return;

            Framebuffer?.Dispose();

            foreach (var view in Attachments.Values) view.Dispose();
            Attachments.Clear();

            foreach (var item in _colorTextures) item.Dispose();
            _colorTextures.Clear();

            _DepthTexture?.Dispose();

            uint width = (uint)SizeEvaluator.Evaluate(_result.Width, DefinitionFile.Game.Window.Width);
            uint height = (uint)SizeEvaluator.Evaluate(_result.Height, DefinitionFile.Game.Window.Height);

            foreach (var item in _result.ColorAttachments)
            {
                var colorDesc = TextureDescription.Texture2D(
                    width, height,
                    mipLevels: item.MipLevels,
                    arrayLayers: item.ArrayLayers,
                    item.Format,
                    TextureUsage.RenderTarget | TextureUsage.Sampled
                );
                Texture colorTex = factory.CreateTexture(ref colorDesc);

                _colorTextures.Add(colorTex);

                ClearColorIDS.Add(item.Clear);
                BlitColorIDS.Add(item.BlitToScreen);

                Attachments.Add(item.Name, factory.CreateTextureView(colorTex));
            }

            if (_result.DepthAttachment != null)
            {
                var depthDesc = TextureDescription.Texture2D(
                    width, height,
                    mipLevels: _result.DepthAttachment.MipLevels,
                    arrayLayers: _result.DepthAttachment.ArrayLayers,
                    _result.DepthAttachment.Format,
                    TextureUsage.DepthStencil | TextureUsage.Sampled
                );
                _DepthTexture = factory.CreateTexture(ref depthDesc);

                ClearDepth = _result.DepthAttachment.Clear;

                if (_result.DepthAttachment.BlitToScreen)
                    BlitDepth = Attachments.Count;

                Attachments.Add(_result.DepthAttachment.Name, factory.CreateTextureView(_DepthTexture));
            }

            var fbDesc = new FramebufferDescription(_DepthTexture, _colorTextures.ToArray());
            
            Framebuffer = factory.CreateFramebuffer(ref fbDesc);
        }
    }

    public class SwapchainFile
    {
        public string Width = "100%";
        public string Height = "100%";

        public List<AttachmentFile> ColorAttachments = new();
        public AttachmentFile DepthAttachment = null!;
    }

    public class AttachmentFile
    {
        public string Name = "";
        public uint MipLevels = 1;
        public uint ArrayLayers = 1;
        public bool Clear = true;
        public PixelFormat Format = PixelFormat.R8_G8_B8_A8_UNorm;
        public bool BlitToScreen = false;
    }
}
