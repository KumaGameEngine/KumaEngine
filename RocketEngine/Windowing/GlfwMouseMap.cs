using KumaEngine.Glfw.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Windowing
{
    public static class GlfwMouseMap
    {
        public static readonly Dictionary<GLFWMouseButton, MouseButton> Map = new()
        {
            [GLFWMouseButton.Left] = MouseButton.Left,
            [GLFWMouseButton.Right] = MouseButton.Right,
            [GLFWMouseButton.Middle] = MouseButton.Middle,
            [GLFWMouseButton.Button1] = MouseButton.Button1,
            [GLFWMouseButton.Button2] = MouseButton.Button2,
            [GLFWMouseButton.Button3] = MouseButton.Button3,
            [GLFWMouseButton.Button4] = MouseButton.Button4,
            [GLFWMouseButton.Button5] = MouseButton.Button5,
            [GLFWMouseButton.Button6] = MouseButton.Button6,
            [GLFWMouseButton.Button7] = MouseButton.Button7,
            [GLFWMouseButton.Button8] = MouseButton.Button8,
        };

        private static readonly Dictionary<MouseButton, GLFWMouseButton> ReverseMap = BuildReverseMap();

        private static Dictionary<MouseButton, GLFWMouseButton> BuildReverseMap()
        {
            var reverse = new Dictionary<MouseButton, GLFWMouseButton>(Map.Count);
            foreach (var (glfwKey, veldridKey) in Map)
                reverse.TryAdd(veldridKey, glfwKey);
            return reverse;
        }

        public static MouseButton ToVeldrid(int glfwKey) =>
            Map.TryGetValue((GLFWMouseButton)glfwKey, out var key) ? key : MouseButton.Left;

        public static ModifierKeys ToVeldridMods(int glfwMods)
        {
            ModifierKeys m = 0;
            if ((glfwMods & 1) != 0) m |= ModifierKeys.Shift;
            if ((glfwMods & 2) != 0) m |= ModifierKeys.Control;
            if ((glfwMods & 4) != 0) m |= ModifierKeys.Alt;
            if ((glfwMods & 8) != 0) m |= ModifierKeys.Gui;
            return m;
        }

        public static GLFWMouseButton ToGlfw(MouseButton veldridKey) =>
            ReverseMap.TryGetValue(veldridKey, out var key) ? key : GLFWMouseButton.Left;
    }
}
