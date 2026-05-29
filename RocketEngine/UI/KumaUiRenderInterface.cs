using Assimp;
using RmlUiNet;
using KumaEngine.Rendering;
using KumaEngine.Rendering.VertexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan;
using static System.Net.Mime.MediaTypeNames;

namespace KumaEngine.UI
{
    public class KumaUiRenderInterface(Game g,KumaPipeline pipeline,CommandList cl) : RenderInterface
    {
        Dictionary<nint, KumaMaterial> UITextures = new();
        Dictionary<nint, Model> Geometry = new();

        public uint GetColor(ColorB color) =>
            color.Blue | ((uint)color.Green << 8) | ((uint)color.Red << 16) | ((uint)color.Alpha << 24);

        public override unsafe nint CompileGeometry(Vertex* vertices, int vertexCount, int* indices, int indexCount)
        {
            var handle = (nint)Random.Shared.Next(int.MinValue, int.MaxValue);

            var rvtx = new Span<Vertex>(vertices,vertexCount);
            var idx = new Span<uint>(indices,indexCount);

            List<ColorUVVertex> vtx = new();

            foreach (var item in rvtx)
                vtx.Add(new(
                    new(item.Position.X, item.Position.Y,0),
                    new(item.TextureCoordinates.X, item.TextureCoordinates.Y),
                    GetColor(item.Colour)
                ));

            var mdl = Model.Create(g.GraphicsDevice,g.ResourceFactory,vtx.ToArray(),idx.ToArray());

            Geometry.Add(handle,mdl);

            return handle;
        }
        public unsafe nint GenerateTextureID(byte* source, int numBytes, Vector2i dimensions,nint handle)
        {
            var factory = g.ResourceFactory;
            var device = g.GraphicsDevice;

            var width = (uint)dimensions.X;
            var height = (uint)dimensions.Y;

            Texture deviceTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled));

            Texture stagingTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Staging));

            device.UpdateTexture(
                    stagingTex,
                    (IntPtr)source,
                    (uint)numBytes,
                    x: 0, y: 0, z: 0,
                    width, height, depth: 1,
                    mipLevel: 0, arrayLayer: 0);

            CommandList cl = factory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(
                stagingTex, 0, 0, 0, 0, 0,
                deviceTex, 0, 0, 0, 0, 0,
                width, height, depth: 1, layerCount: 1);
            cl.End();

            device.SubmitCommands(cl);

            device.DisposeWhenIdle(cl);
            device.DisposeWhenIdle(stagingTex);

            var view = factory.CreateTextureView(deviceTex);

            var mat = new KumaMaterial();
            mat.AddTextureSampler(g.ResourceFactory, "Image", view, g.GraphicsDevice.Aniso4xSampler);

            UITextures.Add(handle, mat);

            return handle;
        }
        public override nint LoadTexture(ref Vector2i textureDimensions, string source)
        {
            var handle = (nint)Random.Shared.Next(int.MinValue, int.MaxValue);

            var view = TextureExt.ViewFromFile(g.GraphicsDevice, g.ResourceFactory, source);
            var s = TextureExt.GetSize(source);

            textureDimensions = new((int)s.X, (int)s.Y);

            var mat = new KumaMaterial();
            mat.AddTextureSampler(g.ResourceFactory, "Image", view, g.GraphicsDevice.Aniso4xSampler);

            UITextures.Add(handle, mat);

            return handle;
        }

        public override unsafe nint GenerateTexture(byte* source, int numBytes, Vector2i dimensions)
        {
            var handle = (nint)Random.Shared.Next(int.MinValue, int.MaxValue);

            return GenerateTextureID(source,numBytes,dimensions, handle);
        }

        public unsafe override void RenderGeometry(nint geometry, Vector2f translation, nint texture)
        {
            var proj = Matrix4x4.CreateOrthographicOffCenter(
                0, g.Window.Width,
                g.Window.Height, 0,
                -1, 1
            );
            var trans = Matrix4x4.CreateTranslation(translation.X, translation.Y, 0);
            cl.UpdateBuffer(pipeline.ModelBuffer, 0, trans * proj);

            if (!UITextures.ContainsKey(0))
            {
                byte[] i = [255, 255, 255, 255];

                fixed (byte* b = i) GenerateTextureID(b, i.Length,new(1,1),0);
            }

            pipeline.Draw(cl, Geometry[geometry], UITextures[texture]);
        }
        public override void ReleaseGeometry(nint geometry)
        {
            Geometry[geometry].Dispose();
            Geometry.Remove(geometry);
        }
        public override void ReleaseTexture(nint textureHandle)
        {
            UITextures[textureHandle].Dispose();
            UITextures.Remove(textureHandle);
        }
    }
}
