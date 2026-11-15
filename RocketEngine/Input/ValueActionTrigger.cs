using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class ValueActionTrigger(DeviceType device, int deviceid, int a, float value) : IActionTrigger
    {
        public float GetValue()
        {
            switch (device)
            {
                case DeviceType.Keyboard:
                    return GetKb(deviceid, a) ? value : 0;
                case DeviceType.Mouse:
                    return GetMouse(deviceid, a) ? value : 0;
                case DeviceType.Gamepad:
                    return GetGamepad(deviceid, a) ? value : 0;
                default: return 0;
            }
        }

        static bool GetKb(int devid, int a)
        {
            if (devid >= InputHandler.Keyboards.Count) return false;

            var window = InputHandler.Keyboards[devid];

            return window.IsKeyPressed((Key)a);
        }

        static bool GetMouse(int devid, int a)
        {
            if (devid >= InputHandler.Mice.Count) return false;

            var window = InputHandler.Mice[devid];

            return window.IsMouseButtonPressed((MouseButton)a);
        }

        static bool GetGamepad(int devid, int a)
        {
            if (devid >= InputHandler.Gamepads.Count) return false;

            var (window, jid) = InputHandler.Gamepads[devid];

            return window.IsGamepadPressed(jid, (GamepadButton)a);
        }
    }
}
