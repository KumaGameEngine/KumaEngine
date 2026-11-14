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
            var (device, window, idx) = InputHandler.Devices[deviceid];

            switch (device)
            {
                case DeviceType.Keyboard:
                    return MapValues(
                        window.IsKeyDown((Key)a), 
                        window.IsKeyDown((Key)b)
                    );
                case DeviceType.Mouse:
                    return MapValues(
                        window.IsMouseButtonDown((MouseButton)a), 
                        window.IsMouseButtonDown((MouseButton)b)
                    );
                case DeviceType.Gamepad:
                    return 0;
                default: return 0;
            }
        }
    }
}
