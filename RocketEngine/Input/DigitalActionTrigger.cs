using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class DigitalActionTrigger(DeviceType device, int deviceid, int a, int b) : IActionTrigger
    {
        static float MapValues(bool a, bool b)
        {
            int av = a ? 1 : 0;
            int bv = b ? 1 : 0;

            return av - bv;
        }

        public float GetValue()
        {
            switch (device)
            {
                case DeviceType.Keyboard:
                    return GetKb(deviceid, a, b);
                case DeviceType.Mouse:
                    return GetMouse(deviceid, a, b);
                case DeviceType.Gamepad:
                    return GetGamepad(deviceid, a, b);
                default: return 0;
            }
        }

        static float GetKb(int devid, int a, int b)
        {
            if (devid >= InputHandler.Keyboards.Count) return 0;

            var window = InputHandler.Keyboards[devid];

            return MapValues(
                window.IsKeyPressed((Key)a),
                window.IsKeyPressed((Key)b)
            );
        }

        static float GetMouse(int devid, int a, int b)
        {
            if (devid >= InputHandler.Mice.Count) return 0;

            var window = InputHandler.Mice[devid];

            return MapValues(
                window.IsMouseButtonPressed((MouseButton)a),
                window.IsMouseButtonPressed((MouseButton)b)
            );
        }

        static float GetGamepad(int devid, int a, int b)
        {
            if (devid >= InputHandler.Gamepads.Count) return 0;

            var (window, jid) = InputHandler.Gamepads[devid];

            return MapValues(
                window.IsGamepadPressed(jid, (GamepadButton)a),
                window.IsGamepadPressed(jid, (GamepadButton)b)
            );
        }
    }
}
