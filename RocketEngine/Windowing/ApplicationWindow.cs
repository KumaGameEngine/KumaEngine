/*
    Taken from: https://github.com/mellinoe/veldrid-samples/tree/master/src/SampleBase
    Modified by: RocketEngine Team
*/

using KumaEngine.Glfw.Source;
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

        void SetCursorMode(CursorMode mode);
        public void SetCursorPosition(Vector2 pos);

        uint Width { get; }
        uint Height { get; }

        public Vector2 Size();
        public Vector2 MouseDelta();
        public Vector2 ScrollDelta();
        public Vector2 MousePosition();

        public float GetJoystickAxis(int jid, GamepadAxis axis);

        public bool IsGamepadPressed(int jid, GamepadButton button);

        public bool IsKeyPressed(Key key);
        public bool IsMouseButtonDown(MouseButton button);
        public bool IsMouseButtonPressed(MouseButton button);
        public bool IsMouseButtonReleased(MouseButton button);

        void Run(GraphicsBackend backend);
    }
}
