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
            var (device, window, idx) = InputHandler.Devices[deviceid];

            switch (device)
            {
                case DeviceType.Keyboard:
                    return window.IsKeyDown((Key)a) ? value : 0;
                case DeviceType.Mouse:
                    return window.IsMouseButtonDown((MouseButton)a) ? value : 0;
                case DeviceType.Gamepad:
                    return 0;
                default: return 0;
            }
        }
    }
}
