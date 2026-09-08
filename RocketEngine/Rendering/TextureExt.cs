using StbImageSharp;
using System;
using System.IO;
using System.Numerics;
using Veldrid;
using Vortice.DXGI;
using static System.Net.Mime.MediaTypeNames;

namespace KumaEngine.Rendering
{
    public static class TextureExt
    {
        static TextureView NullTexture = null!;

        static Dictionary<string,TextureView> _viewCache = new();

        public static void InvalidateCache() => _viewCache.Clear();

        public static Vector2 GetSize(string file)
        {
            var defpath = AssetRetriver.FetchAssetPath(AssetKind.Textures, file);

            if (_viewCache.TryGetValue(defpath, out var cv))
                return new(cv.Target.Width, cv.Target.Height);

            ImageResult image;
            using (var stream = File.OpenRead(defpath))
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            uint width = (uint)image.Width;
            uint height = (uint)image.Height;

            return new(width,height);
        }

        public static TextureView GetNullTexture(GraphicsDevice device, ResourceFactory factory)
        {
            if (NullTexture != null) return NullTexture;

            uint width = 1;
            uint height = 1;

            Texture deviceTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
                    TextureUsage.Sampled));

            Texture stagingTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
                    TextureUsage.Staging));

            uint[] data = [0xFF00FF];

            device.UpdateTexture(
                stagingTex,
                data,
                x: 0, y: 0, z: 0,
                width, height, depth: 1,
                mipLevel: 0, arrayLayer: 0
            );

            CommandList cl = factory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(
                stagingTex, 0, 0, 0, 0, 0,
                deviceTex, 0, 0, 0, 0, 0,
                width, height, depth: 1, layerCount: 1
            );
            cl.End();

            device.SubmitCommands(cl);

            device.DisposeWhenIdle(cl);
            device.DisposeWhenIdle(stagingTex);

            NullTexture = factory.CreateTextureView(deviceTex);

            return NullTexture;
        }

        public static TextureView ViewFromFile(GraphicsDevice device, ResourceFactory factory, string file, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm_SRgb)
        {
            var defpath = AssetRetriver.FetchAssetPath(AssetKind.Textures, file);

            if (_viewCache.TryGetValue(defpath, out var cv))
                return cv;

            ImageResult image;
            using (var stream = File.OpenRead(defpath))
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            uint width = (uint)image.Width;
            uint height = (uint)image.Height;

            Texture deviceTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    format,
                    TextureUsage.Sampled));

            Texture stagingTex = factory.CreateTexture(
                TextureDescription.Texture2D(
                    width, height,
                    mipLevels: 1,
                    arrayLayers: 1,
                    format,
                    TextureUsage.Staging));

            unsafe
            {
                fixed (byte* ptr = image.Data)
                {
                    device.UpdateTexture(
                        stagingTex,
                        (IntPtr)ptr,
                        (uint)image.Data.Length,
                        x: 0, y: 0, z: 0,
                        width, height, depth: 1,
                        mipLevel: 0, arrayLayer: 0
                    );
                }
            }

            CommandList cl = factory.CreateCommandList();
            cl.Begin();
            cl.CopyTexture(
                stagingTex, 0, 0, 0, 0, 0,
                deviceTex, 0, 0, 0, 0, 0,
                width, height, depth: 1, layerCount: 1
            );
            cl.End();

            device.SubmitCommands(cl);

            device.DisposeWhenIdle(cl);
            device.DisposeWhenIdle(stagingTex);

            var view = factory.CreateTextureView(deviceTex);

            _viewCache.Add(defpath,view);

            return view;
        }

        public static TextureView CubemapFromFile(
            GraphicsDevice device, ResourceFactory factory, string file, PixelFormat format = PixelFormat.R8_G8_B8_A8_UNorm_SRgb)
        {
            var path = AssetRetriver.FetchAssetPath(AssetKind.Textures, file);

            if (_viewCache.TryGetValue(path, out var cv))
                return cv;

            ImageResult image;
            using (var stream = File.OpenRead(path))
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            int faceSize;
            (int x, int y)[] faces;

            int w = image.Width;
            int h = image.Height;

            if (w / 6 == h && w % 6 == 0)
            {
                faceSize = h;
                faces = new[]
                {
                    (0 * faceSize, 0),
                    (1 * faceSize, 0),
                    (2 * faceSize, 0),
                    (3 * faceSize, 0),
                    (4 * faceSize, 0),
                    (5 * faceSize, 0),
                };
            }
            else if (h / 6 == w && h % 6 == 0)
            {
                faceSize = w;
                faces = new[]
                {
                    (0, 0 * faceSize),
                    (0, 1 * faceSize),
                    (0, 2 * faceSize),
                    (0, 3 * faceSize),
                    (0, 4 * faceSize),
                    (0, 5 * faceSize),
                };
            }
            else if (w / 4 == h / 3 && w % 4 == 0 && h % 3 == 0)
            {
                faceSize = w / 4;
                faces = new[]
                {
                    (2 * faceSize, 1 * faceSize),
                    (0 * faceSize, 1 * faceSize),
                    (1 * faceSize, 0 * faceSize),
                    (1 * faceSize, 2 * faceSize),
                    (1 * faceSize, 1 * faceSize),
                    (3 * faceSize, 1 * faceSize),
                };
            }
            else if (w / 3 == h / 4 && w % 3 == 0 && h % 4 == 0)
            {
                faceSize = w / 3;
                faces = new[]
                {
                    (2 * faceSize, 1 * faceSize),
                    (0 * faceSize, 1 * faceSize),
                    (1 * faceSize, 0 * faceSize),
                    (1 * faceSize, 2 * faceSize),
                    (1 * faceSize, 1 * faceSize),
                    (1 * faceSize, 3 * faceSize),
                };
            }
            else
            {
                throw new InvalidDataException(
                    $"Cubemap '{file}' has unrecognised layout ({w}×{h}). " +
                    "Expected a 6:1 / 1:6 strip or a 4:3 / 3:4 cross.");
            }

            uint uFace = (uint)faceSize;

            Texture deviceTex = factory.CreateTexture(
                TextureDescription.Texture2D(uFace, uFace, 1, arrayLayers: 6,
                    format,
                    TextureUsage.Sampled | TextureUsage.Cubemap
                )
            );

            Texture stagingTex = factory.CreateTexture(
                TextureDescription.Texture2D(uFace, uFace, 1, arrayLayers: 6,
                    format,
                    TextureUsage.Staging
                )
            );

            byte[] facePixels = new byte[faceSize * faceSize * 4];

            unsafe
            {
                fixed (byte* facePtr = facePixels)
                {
                    for (uint layer = 0; layer < 6; layer++)
                    {
                        ExtractFace(image.Data, w, faces[layer].x, faces[layer].y,
                                    faceSize, facePixels);

                        device.UpdateTexture(stagingTex, (IntPtr)facePtr, (uint)facePixels.Length,
                            0, 0, 0, uFace, uFace, 1,
                            mipLevel: 0, arrayLayer: layer
                        );
                    }
                }
            }

            CommandList cl = factory.CreateCommandList();
            cl.Begin();
            for (uint layer = 0; layer < 6; layer++)
            {
                cl.CopyTexture(
                    stagingTex, 0, 0, 0, 0, layer,
                    deviceTex, 0, 0, 0, 0, layer,
                    uFace, uFace, 1, layerCount: 1
                );
            }
            cl.End();
            device.SubmitCommands(cl);
            device.DisposeWhenIdle(cl);
            device.DisposeWhenIdle(stagingTex);

            var view = factory.CreateTextureView(
                new TextureViewDescription(
                    deviceTex,
                    baseMipLevel: 0, mipLevels: 1,
                    baseArrayLayer: 0, arrayLayers: 6
                )
            );

            _viewCache.Add(path, view);

            return view;
        }

        private static void ExtractFace(
            byte[] src, int srcWidth,
            int originX, int originY, int faceSize,
            byte[] dst)
        {
            int stride = faceSize * 4;
            for (int row = 0; row < faceSize; row++)
            {
                int srcOffset = ((originY + row) * srcWidth + originX) * 4;
                int dstOffset = row * stride;
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, stride);
            }
        }
    }
}