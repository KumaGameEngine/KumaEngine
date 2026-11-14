using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class DigitalActionTrigger(int deviceid, int a, int b) : IActionTrigger
    {
        static float MapValues(bool a, bool b)
        {
            int av = a ? 1 : 0;
            int bv = b ? 1 : 0;

            return av - bv;
        }

        public float GetValue()
        {
            if (!InputHandler.Devices.ContainsKey(deviceid)) return 0;

            var (device, window, idx) = InputHandler.Devices[deviceid];

            switch (device)
            {
                case DeviceType.Keyboard:
                    return MapValues(
                        window.IsKeyPressed((Key)a), 
                        window.IsKeyPressed((Key)b)
                    );
                case DeviceType.Mouse:
                    return MapValues(
                        window.IsMouseButtonPressed((MouseButton)a),
                        window.IsMouseButtonPressed((MouseButton)b)
                    );
                case DeviceType.Gamepad:
                    return MapValues(
                        window.IsGamepadPressed(idx, (GamepadButton)a),
                        window.IsGamepadPressed(idx, (GamepadButton)b)
                    );
                default: return 0;
            }
        }
    }
}
