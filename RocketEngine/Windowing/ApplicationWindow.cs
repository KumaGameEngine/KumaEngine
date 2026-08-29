/*
    Taken from: https://github.com/mellinoe/veldrid-samples/tree/master/src/SampleBase
    Modified by: RocketEngine Team
*/

using System;
using System.Numerics;
using Veldrid;

namespace KumaEngine.Windowing
{
    public interface ApplicationWindow
    {
        PlatformType PlatformType { get; }

        event Action<float> Rendering;
        event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        event Action GraphicsDeviceDestroyed;
        event Action Resized;
        event Action Closing;
        event Action<KeyEvent> KeyPressed;

        void HideCursor(bool hiding);
        void RelativeCursor(bool relative);
        void CenterCursor();
        public void SetCursorPosition(Vector2 pos);

        uint Width { get; }
        uint Height { get; }

        public Vector2 Size();
        public Vector2 MouseDelta();

        void Run(GraphicsBackend backend);
    }
}
