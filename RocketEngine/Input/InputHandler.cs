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
        public static List<ApplicationWindow> Keyboards { get; private set; } = new();
        public static List<ApplicationWindow> Mice { get; private set; } = new();
        public static List<(ApplicationWindow window, int jid)> Gamepads { get; private set; } = new();

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

        public static int DeviceRemoved(DeviceType dev, int devid)
        {
            switch (dev)
            {
                case DeviceType.Keyboard:
                    Keyboards.RemoveAt(devid); break;
                case DeviceType.Mouse:
                    Mice.RemoveAt(devid); break;
                case DeviceType.Gamepad:
                    Gamepads.RemoveAt(devid); break;
            }

            return devid;
        }

        public static int DeviceConnected(DeviceType dev, ApplicationWindow window, int idx = 0)
        {
            switch (dev)
            {
                case DeviceType.Keyboard:
                    Keyboards.Add(window);
                    return Keyboards.Count - 1;
                case DeviceType.Mouse:
                    Mice.Add(window);
                    return Mice.Count - 1;
                case DeviceType.Gamepad:
                    Gamepads.Add((window, idx));
                    return Gamepads.Count - 1;
            }

            return -1;
        }
    }
}
