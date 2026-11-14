using KumaEngine.Glfw.Source;
using KumaEngine.GLFW.Native;
using KumaEngine.Input;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;
using glfw = KumaEngine.Glfw.Source.Glfw;
using Glfw3Window = KumaEngine.Glfw.Source.Window;

namespace KumaEngine.Windowing
{
    public unsafe class GlfwWindow : ApplicationWindow
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandleW(string? lpModuleName);

        static Dictionary<IntPtr, GlfwWindow> windowPins = new();

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void FramebufferSizeTrampoline(GLFWwindow* window, int width, int height)
        {
            if (windowPins.TryGetValue((IntPtr)window, out var w))
            {
                w._windowResized = true;
                w.Width = (uint)width;
                w.Height = (uint)height;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void KeyTrampoline(GLFWwindow* window, int key, int scancode, int action, int mods)
        {
            if (windowPins.TryGetValue((IntPtr)window, out var w))
                w.OnKeyDown(key, scancode, action, mods);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void CursorPosTrampoline(GLFWwindow* window, double xpos, double ypos)
        {
            if (windowPins.TryGetValue((IntPtr)window, out var w))
            {
                var ppos = w.MousePos;
                w.MousePos = new((float)xpos, (float)ypos);

                w._mouseDelta = w.MousePos - ppos;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void CursorScrollTrampoline(GLFWwindow* window, double xscroll, double yscroll)
        {
            if (windowPins.TryGetValue((IntPtr)window, out var w))
            {
                w._scrollDelta = new((float)xscroll, (float)yscroll);
            }
        }

        public Glfw3Window _window;
        private GraphicsDevice _gd;
        private DisposeCollectorResourceFactory _factory;
        private bool _windowResized = true;
        private Vector2 _mouseDelta = Vector2.Zero;
        private Vector2 _scrollDelta = Vector2.Zero;

        List<MouseButton> _heldButtons = new();

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;
        public event Action Closing;
        public event Action<KeyEvent> KeyPressed;

        public Vector2 MousePos { get; private set; } = Vector2.Zero;

        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public PlatformType PlatformType => PlatformType.Desktop;

        public GlfwWindow(string title)
        {
            glfw.Init();
            glfw.SetWindowHint(GLFWContextHint.ClientApi, (int)GLFWClientApi.NoApi);

            _window = glfw.CreateWindow(1280,720,title);
            windowPins.Add((IntPtr)_window.Handle, this);

            _window.SetFramebufferSizeCallback(&FramebufferSizeTrampoline);
            _window.SetKeyCallback(&KeyTrampoline);
            _window.SetCursorPosCallback(&CursorPosTrampoline);
            GLFWNative.glfwSetScrollCallback(_window.Handle, &CursorScrollTrampoline);

            var size = _window.FramebufferSize;

            Width = (uint)size.X;
            Height = (uint)size.Y;

            MousePos = _window.CursorPos;
        }

        public void Run(GraphicsBackend backend)
        {
            InputHandler.DeviceConnected(DeviceType.Keyboard, this, 0);
            InputHandler.DeviceConnected(DeviceType.Mouse, this, 0);

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: null,
                syncToVerticalBlank: false,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true
            );
#if DEBUG
            options.Debug = true;
#endif

            IntPtr whandle = _window.GetNativeWindowHandle();

            SwapchainSource swap = glfw.GetPlatform() switch
            {
                GLFWPlatform.Win32 => SwapchainSource.CreateWin32(whandle, GetModuleHandleW(null)),
                GLFWPlatform.Cocoa => SwapchainSource.CreateNSWindow(whandle),
                GLFWPlatform.X11 => SwapchainSource.CreateXlib((IntPtr)GLFWNativePlatform.glfwGetX11Display(), whandle),
                GLFWPlatform.Wayland => SwapchainSource.CreateWayland((IntPtr)GLFWNativePlatform.glfwGetWaylandDisplay(), whandle),
                _ => throw new Exception("Platform not supported")
            };

            SwapchainDescription scDesc = new SwapchainDescription(
                source: swap,
                width: Width,
                height: Height,
                depthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: false,
                colorSrgb: true
            );

            _gd = backend switch 
            {
                GraphicsBackend.Vulkan => GraphicsDevice.CreateVulkan(options, scDesc),
                GraphicsBackend.Metal => GraphicsDevice.CreateMetal(options, scDesc),
                _ => GraphicsDevice.CreateD3D11(options, scDesc),
            };
            
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
            GraphicsDeviceCreated?.Invoke(_gd, _factory, _gd.MainSwapchain);

            Stopwatch sw = Stopwatch.StartNew();
            double previousElapsed = sw.Elapsed.TotalSeconds;

            while (!_window.ShouldClose)
            {
                glfw.PollEvents();

                double newElapsed = sw.Elapsed.TotalSeconds;
                float deltaSeconds = (float)(newElapsed - previousElapsed);

                if (!_window.ShouldClose)
                {
                    previousElapsed = newElapsed;
                    if (_windowResized)
                    {
                        _windowResized = false;
                        _gd.ResizeMainWindow(Width, Height);
                        Resized?.Invoke();
                    }

                    Rendering?.Invoke(deltaSeconds);
                }

                _mouseDelta = Vector2.Zero;
                _scrollDelta = Vector2.Zero;
            }

            Closing?.Invoke();

            _gd.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            _gd.Dispose();
            GraphicsDeviceDestroyed?.Invoke();
        }

        protected void OnKeyDown(int key, int scancode, int action, int mods)
        {
            Key k = GlfwKeyMap.ToVeldrid(key);
            ModifierKeys md = GlfwKeyMap.ToVeldridMods(mods);

            bool down = action != (int)GLFWAction.Release;
            bool repeat = action == (int)GLFWAction.Repeat;

            KeyPressed?.Invoke(new(k, down, md, repeat));
        }

        public bool IsKeyDown(Key key) => _window.GetKey(GlfwKeyMap.ToGlfw(key)) == GLFWAction.Press;
        public bool IsMouseButtonPressed(MouseButton button) => _window.GetMouseButton(GlfwMouseMap.ToGlfw(button)) == GLFWAction.Press;

        public bool IsMouseButtonDown(MouseButton button)
        {
            var down = IsMouseButtonPressed(button);

            if (!down && _heldButtons.Contains(button))
                _heldButtons.Remove(button);

            if (down && !_heldButtons.Contains(button))
            {
                _heldButtons.Add(button);
                return true;
            }

            return false;
        }

        public bool IsMouseButtonReleased(MouseButton button)
        {
            var down = IsMouseButtonPressed(button);

            if (down && !_heldButtons.Contains(button))
                _heldButtons.Add(button);

            if (!down && _heldButtons.Contains(button))
            {
                _heldButtons.Remove(button);
                return true;
            }

            return false;
        }

        public void SetCursorMode(CursorMode mode)
        {
            _window.SetCursorMode((GLFWCursorMode)(mode + (int)GLFWCursorMode.Normal));

            if (GLFWNative.glfwRawMouseMotionSupported() != 0)
                GLFWNative.glfwSetInputMode(
                    _window.Handle, 
                    (int)GLFWInputMode.RawMouseMotion , 
                    (mode == CursorMode.Disabled) ? 1 : 0
                );
        }

        public Vector2 Size() => new Vector2(Width, Height);
        public Vector2 MouseDelta() => _mouseDelta;
        public Vector2 ScrollDelta() => _scrollDelta;
        public Vector2 MousePosition() => MousePos;

        public void SetCursorPosition(Vector2 pos) => _window.CursorPos = pos;
    }
}
