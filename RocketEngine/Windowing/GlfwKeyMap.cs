using KumaEngine.Glfw.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Windowing
{
    public static class GlfwKeyMap
    {
        public static readonly Dictionary<GLFWKey, Key> Map = new()
        {
            [GLFWKey.Unknown] = Key.Unknown,

            [GLFWKey.Space] = Key.Space,
            [GLFWKey.Apostrophe] = Key.Quote,
            [GLFWKey.Comma] = Key.Comma,
            [GLFWKey.Minus] = Key.Minus,
            [GLFWKey.Period] = Key.Period,
            [GLFWKey.Slash] = Key.Slash,
            [GLFWKey.Semicolon] = Key.Semicolon,
            [GLFWKey.Equal] = Key.Plus,
            [GLFWKey.LeftBracket] = Key.BracketLeft,
            [GLFWKey.Backslash] = Key.BackSlash,
            [GLFWKey.RightBracket] = Key.BracketRight,
            [GLFWKey.GraveAccent] = Key.Tilde,
            [GLFWKey.World1] = Key.NonUSBackSlash,

            [GLFWKey.Num0] = Key.Number0,
            [GLFWKey.Num1] = Key.Number1,
            [GLFWKey.Num2] = Key.Number2,
            [GLFWKey.Num3] = Key.Number3,
            [GLFWKey.Num4] = Key.Number4,
            [GLFWKey.Num5] = Key.Number5,
            [GLFWKey.Num6] = Key.Number6,
            [GLFWKey.Num7] = Key.Number7,
            [GLFWKey.Num8] = Key.Number8,
            [GLFWKey.Num9] = Key.Number9,

            [GLFWKey.A] = Key.A,
            [GLFWKey.B] = Key.B,
            [GLFWKey.C] = Key.C,
            [GLFWKey.D] = Key.D,
            [GLFWKey.E] = Key.E,
            [GLFWKey.F] = Key.F,
            [GLFWKey.G] = Key.G,
            [GLFWKey.H] = Key.H,
            [GLFWKey.I] = Key.I,
            [GLFWKey.J] = Key.J,
            [GLFWKey.K] = Key.K,
            [GLFWKey.L] = Key.L,
            [GLFWKey.M] = Key.M,
            [GLFWKey.N] = Key.N,
            [GLFWKey.O] = Key.O,
            [GLFWKey.P] = Key.P,
            [GLFWKey.Q] = Key.Q,
            [GLFWKey.R] = Key.R,
            [GLFWKey.S] = Key.S,
            [GLFWKey.T] = Key.T,
            [GLFWKey.U] = Key.U,
            [GLFWKey.V] = Key.V,
            [GLFWKey.W] = Key.W,
            [GLFWKey.X] = Key.X,
            [GLFWKey.Y] = Key.Y,
            [GLFWKey.Z] = Key.Z,

            [GLFWKey.Escape] = Key.Escape,
            [GLFWKey.Enter] = Key.Enter,
            [GLFWKey.Tab] = Key.Tab,
            [GLFWKey.Backspace] = Key.BackSpace,
            [GLFWKey.Insert] = Key.Insert,
            [GLFWKey.Delete] = Key.Delete,
            [GLFWKey.Right] = Key.Right,
            [GLFWKey.Left] = Key.Left,
            [GLFWKey.Down] = Key.Down,
            [GLFWKey.Up] = Key.Up,
            [GLFWKey.PageUp] = Key.PageUp,
            [GLFWKey.PageDown] = Key.PageDown,
            [GLFWKey.Home] = Key.Home,
            [GLFWKey.End] = Key.End,

            [GLFWKey.CapsLock] = Key.CapsLock,
            [GLFWKey.ScrollLock] = Key.ScrollLock,
            [GLFWKey.NumLock] = Key.NumLock,
            [GLFWKey.PrintScreen] = Key.PrintScreen,
            [GLFWKey.Pause] = Key.Pause,
            [GLFWKey.Menu] = Key.Menu,

            [GLFWKey.F1] = Key.F1,
            [GLFWKey.F2] = Key.F2,
            [GLFWKey.F3] = Key.F3,
            [GLFWKey.F4] = Key.F4,
            [GLFWKey.F5] = Key.F5,
            [GLFWKey.F6] = Key.F6,
            [GLFWKey.F7] = Key.F7,
            [GLFWKey.F8] = Key.F8,
            [GLFWKey.F9] = Key.F9,
            [GLFWKey.F10] = Key.F10,
            [GLFWKey.F11] = Key.F11,
            [GLFWKey.F12] = Key.F12,
            [GLFWKey.F13] = Key.F13,
            [GLFWKey.F14] = Key.F14,
            [GLFWKey.F15] = Key.F15,
            [GLFWKey.F16] = Key.F16,
            [GLFWKey.F17] = Key.F17,
            [GLFWKey.F18] = Key.F18,
            [GLFWKey.F19] = Key.F19,
            [GLFWKey.F20] = Key.F20,
            [GLFWKey.F21] = Key.F21,
            [GLFWKey.F22] = Key.F22,
            [GLFWKey.F23] = Key.F23,
            [GLFWKey.F24] = Key.F24,
            [GLFWKey.F25] = Key.F25,

            [GLFWKey.Keypad0] = Key.Keypad0,
            [GLFWKey.Keypad1] = Key.Keypad1,
            [GLFWKey.Keypad2] = Key.Keypad2,
            [GLFWKey.Keypad3] = Key.Keypad3,
            [GLFWKey.Keypad4] = Key.Keypad4,
            [GLFWKey.Keypad5] = Key.Keypad5,
            [GLFWKey.Keypad6] = Key.Keypad6,
            [GLFWKey.Keypad7] = Key.Keypad7,
            [GLFWKey.Keypad8] = Key.Keypad8,
            [GLFWKey.Keypad9] = Key.Keypad9,
            [GLFWKey.KeypadDecimal] = Key.KeypadDecimal,
            [GLFWKey.KeypadDivide] = Key.KeypadDivide,
            [GLFWKey.KeypadMultiply] = Key.KeypadMultiply,
            [GLFWKey.KeypadSubtract] = Key.KeypadSubtract,
            [GLFWKey.KeypadAdd] = Key.KeypadAdd,
            [GLFWKey.KeypadEnter] = Key.KeypadEnter,

            [GLFWKey.LeftShift] = Key.ShiftLeft,
            [GLFWKey.LeftControl] = Key.ControlLeft,
            [GLFWKey.LeftAlt] = Key.AltLeft,
            [GLFWKey.LeftSuper] = Key.WinLeft,
            [GLFWKey.RightShift] = Key.ShiftRight,
            [GLFWKey.RightControl] = Key.ControlRight,
            [GLFWKey.RightAlt] = Key.AltRight,
            [GLFWKey.RightSuper] = Key.WinRight,
        };

        private static readonly Dictionary<Key, GLFWKey> ReverseMap = BuildReverseMap();

        private static Dictionary<Key, GLFWKey> BuildReverseMap()
        {
            var reverse = new Dictionary<Key, GLFWKey>(Map.Count);
            foreach (var (glfwKey, veldridKey) in Map)
                reverse.TryAdd(veldridKey, glfwKey);
            return reverse;
        }

        public static Key ToVeldrid(int glfwKey) =>
            Map.TryGetValue((GLFWKey)glfwKey, out var key) ? key : Key.Unknown;

        public static ModifierKeys ToVeldridMods(int glfwMods)
        {
            ModifierKeys m = 0;
            if ((glfwMods & 1) != 0) m |= ModifierKeys.Shift;
            if ((glfwMods & 2) != 0) m |= ModifierKeys.Control;
            if ((glfwMods & 4) != 0) m |= ModifierKeys.Alt;
            if ((glfwMods & 8) != 0) m |= ModifierKeys.Gui;
            return m;
        }

        public static GLFWKey ToGlfw(Key veldridKey) =>
            ReverseMap.TryGetValue(veldridKey, out var key) ? key : GLFWKey.Unknown;
    }
}
