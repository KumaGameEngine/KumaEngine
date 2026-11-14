using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class AnalogActionTrigger(int deviceid, GamepadAxis axis, float deadzone) : IActionTrigger
    {
        public float GetValue()
        {
            if (!InputHandler.Devices.ContainsKey(deviceid)) return 0;

            var (device, window, idx) = InputHandler.Devices[deviceid];

            if (device == DeviceType.Gamepad)
            {
                var val = window.GetJoystickAxis(idx, axis);

                if (Math.Abs(val) > deadzone) return val;
            }

            return 0;
        }
    }
}
