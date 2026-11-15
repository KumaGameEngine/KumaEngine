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
            if (deviceid >= InputHandler.Gamepads.Count) return 0;

            var (window, idx) = InputHandler.Gamepads[deviceid];

            var val = window.GetJoystickAxis(idx, axis);

            if (Math.Abs(val) > deadzone) return val;

            return 0;
        }
    }
}
