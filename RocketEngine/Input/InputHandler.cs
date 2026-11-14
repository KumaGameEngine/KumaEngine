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
        public static Dictionary<int, (DeviceType dev, ApplicationWindow window, int idx)> Devices { get; private set; } = new();

        static Dictionary<string, InputAction> Actions = new();

        public static void RegisterAction(string name, InputAction action) => Actions[name] = action;

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

        public static int DeviceRemoved(int devid)
        {
            Devices.Remove(devid);
            return devid;
        }

        public static int DeviceConnected(DeviceType dev, ApplicationWindow window, int idx)
        {
            var id = Random.Shared.Next();
            Devices[id] = (dev, window, idx);

            return id;
        }
    }
}
