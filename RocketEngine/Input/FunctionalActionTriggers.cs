using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Input
{
    public class InverseActionTrigger(IActionTrigger trigger) : IActionTrigger
    {
        public float GetValue() => -trigger.GetValue();
    }

    public class NOTActionTrigger(IActionTrigger trigger) : IActionTrigger
    {
        public float GetValue() => trigger.GetValue() != 0 ? 1 : 0;
    }

    public class ANDActionTrigger(IActionTrigger triggerA, IActionTrigger triggerB) : IActionTrigger
    {
        public float GetValue() => (triggerA.GetValue() != 0 && triggerB.GetValue() != 0) ? 1 : 0;
    }

    public class ORActionTrigger(IActionTrigger triggerA, IActionTrigger triggerB) : IActionTrigger
    {
        public float GetValue() => (triggerA.GetValue() != 0 || triggerB.GetValue() != 0) ? 1 : 0;
    }

    public class XORActionTrigger(IActionTrigger triggerA, IActionTrigger triggerB) : IActionTrigger
    {
        public float GetValue() => (triggerA.GetValue() != 0 ^ triggerB.GetValue() != 0) ? 1 : 0;
    }
}
