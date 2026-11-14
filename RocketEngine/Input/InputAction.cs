using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Input
{
    public class InputAction
    {
        public List<IActionTrigger> InputTriggers { get; set; } = new();

        bool wasDown = false;

        public float GetValue()
        {
            float value = 0;
            foreach (var trigger in InputTriggers)
            {
                value += trigger.GetValue();
            }
            return value;
        }

        public bool IsTriggerPressed()
        {
            foreach (var trigger in InputTriggers)
            {
                if (trigger.GetValue() != 0)
                    return true;
            }
            return false;
        }

        public bool IsTriggerDown()
        {
            var down = IsTriggerPressed();

            if (down && !wasDown)
            {
                wasDown = true;
                return true;
            }

            wasDown = down;

            return false;
        }

        public bool IsTriggerReleased()
        {
            var down = IsTriggerPressed();

            if (!down && wasDown)
            {
                wasDown = false;
                return true;
            }

            wasDown = down;

            return false;
        }
    }
}
