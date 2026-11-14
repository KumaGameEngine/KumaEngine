using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class ValueActionTrigger(int deviceid, int a, float value) : IActionTrigger
    {
        public float GetValue()
        {
            if (!InputHandler.Devices.ContainsKey(deviceid)) return 0;

            var (device, window, idx) = InputHandler.Devices[deviceid];

            switch (device)
            {
                case DeviceType.Keyboard:
                    return window.IsKeyPressed((Key)a) ? value : 0;
                case DeviceType.Mouse:
                    return window.IsMouseButtonDown((MouseButton)a) ? value : 0;
                case DeviceType.Gamepad:
                    return window.IsGamepadPressed(idx, (GamepadButton)a) ? value : 0;
                default: return 0;
            }
        }
    }
}
