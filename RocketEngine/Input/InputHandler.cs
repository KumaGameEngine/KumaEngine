using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Input
{
    public static class InputHandler
    {
        public static List<(DeviceType dev, ApplicationWindow window, int idx)> Devices { get; private set; } = new();

        static Dictionary<string, InputAction> Actions = new();

        public static void RegisterAction(string name, InputAction action)
        {
            if (!Actions.ContainsKey(name))
                Actions.Add(name, action);
        }

        public static float GetActionValue(string name)
        {
            if (Actions.ContainsKey(name))
                return Actions[name].GetValue();
            return 0;
        }
        public static bool IsActionPressed(string name)
        {
            if (Actions.ContainsKey(name))
                return Actions[name].IsTriggerPressed();
            return false;
        }
        public static bool IsActionDown(string name)
        {
            if (Actions.ContainsKey(name))
                return Actions[name].IsTriggerDown();
            return false;
        }
        public static bool IsActionReleased(string name)
        {
            if (Actions.ContainsKey(name))
                return Actions[name].IsTriggerReleased();
            return false;
        }

        public static void DeviceRemoved(int devid) => Devices.RemoveAt(devid);

        public static void DeviceConnected(DeviceType dev, ApplicationWindow window, int idx) =>
            Devices.Add((dev, window, idx));
    }
}
