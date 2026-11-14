using KeraLua;
using KumaEngine.Input;
using KumaEngine.Rendering;
using KumaEngine.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan.Xlib;

namespace KumaEngine.API
{
    public static class InputAPI
    {
        public static Dictionary<int, IActionTrigger> TriggerHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("registerAction",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var action = lua.ToString(1);
                var triggers = lua.ToIntArray(2);

                var ia = new InputAction();

                foreach (var item in triggers)
                    ia.InputTriggers.Add(TriggerHandles[item]);

                InputHandler.RegisterAction(action, ia);

                return 0;
            }),
            DefinitionFile.RegisterFunction("createDigitalActionTrigger",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceid = (int)lua.ToInteger(1);
                var keyA = (int)lua.ToInteger(2);
                var keyB = (int)lua.ToInteger(3);

                lua.PushActionTrigger(new DigitalActionTrigger(deviceid, keyA, keyB));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createValueActionTrigger",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceid = (int)lua.ToInteger(1);
                var keyA = (int)lua.ToInteger(2);
                var value = (float)lua.ToNumber(3);

                lua.PushActionTrigger(new ValueActionTrigger(deviceid, keyA, value));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createBooleanActionTrigger",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceid = (int)lua.ToInteger(1);
                var keyA = (int)lua.ToInteger(2);

                lua.PushActionTrigger(new ValueActionTrigger(deviceid, keyA, 1));

                return 1;
            }),

            DefinitionFile.RegisterFunction("getKeyboardDevice",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceidx = (int)lua.ToInteger(1);

                var keyboardIndices = InputHandler.Devices
                    .Select((d, i) => (d, i))
                    .Where(x => x.d.dev == DeviceType.Keyboard)
                    .Select(x => x.i)
                    .ToArray();

                if (keyboardIndices.Length == 0)
                {
                    lua.PushNil();
                    return 1;
                }

                lua.PushInteger(keyboardIndices[Math.Clamp(deviceidx, 0, keyboardIndices.Length - 1)]);

                return 1;
            }),

            DefinitionFile.RegisterFunction("getMouseDevice",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceidx = (int)lua.ToInteger(1);

                var keyboardIndices = InputHandler.Devices
                    .Select((d, i) => (d, i))
                    .Where(x => x.d.dev == DeviceType.Mouse)
                    .Select(x => x.i)
                    .ToArray();

                if (keyboardIndices.Length == 0)
                {
                    lua.PushNil();
                    return 1;
                }

                lua.PushInteger(keyboardIndices[Math.Clamp(deviceidx, 0, keyboardIndices.Length - 1)]);

                return 1;
            }),

            DefinitionFile.RegisterFunction("getGamepadDevice",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var deviceidx = (int)lua.ToInteger(1);

                var keyboardIndices = InputHandler.Devices
                    .Select((d, i) => (d, i))
                    .Where(x => x.d.dev == DeviceType.Gamepad)
                    .Select(x => x.i)
                    .ToArray();

                if (keyboardIndices.Length == 0)
                {
                    lua.PushNil();
                    return 1;
                }

                lua.PushInteger(keyboardIndices[Math.Clamp(deviceidx, 0, keyboardIndices.Length - 1)]);

                return 1;
            }),

            DefinitionFile.RegisterFunction("cursorMode",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var b = lua.ToInteger(1);

                DefinitionFile.Game.Window.SetCursorMode((CursorMode)b);

                return 1;
            }),
            DefinitionFile.RegisterFunction("getActionValue",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToString(1);

                lua.PushNumber(InputHandler.GetActionValue(key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isActionDown",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToString(1);

                lua.PushBoolean(InputHandler.IsActionDown(key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isActionDown",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToString(1);

                lua.PushBoolean(InputHandler.IsActionDown(key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isActionPressed",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToString(1);

                lua.PushBoolean(InputHandler.IsActionPressed(key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isActionReleased",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToString(1);

                lua.PushBoolean(InputHandler.IsActionReleased(key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isMouseDown",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToInteger(1);

                lua.PushBoolean(DefinitionFile.Game.Window.IsMouseButtonDown((MouseButton)key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isMouseReleased",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToInteger(1);

                lua.PushBoolean(DefinitionFile.Game.Window.IsMouseButtonReleased((MouseButton)key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isMousePressed",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToInteger(1);

                lua.PushBoolean(DefinitionFile.Game.Window.IsMouseButtonPressed((MouseButton)key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("getMousePosition",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushVec3(new(DefinitionFile.Game.Window.MousePosition(),0));

                return 1;
            }),
            DefinitionFile.RegisterFunction("getScrollDelta",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushVec3(new(DefinitionFile.Game.Window.ScrollDelta(),0));

                return 1;
            }),
            DefinitionFile.RegisterFunction("getMousePositionDelta",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushVec3(new(DefinitionFile.Game.Window.MouseDelta(),0));

                return 1;
            })
        ];

        public static void PushActionTrigger(this Lua lua, IActionTrigger mat)
        {
            var handle = 0;

            if (TriggerHandles.ContainsValue(mat))
                handle = TriggerHandles.First(x => x.Value == mat).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                TriggerHandles.Add(handle, mat);
            }

            lua.PushInteger(handle);
        }

        public static IActionTrigger ToActionTrigger(this Lua lua, int index)
        {
            int val = (int)lua.ToInteger(index);
            if (TriggerHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid trigger handle");
        }
    }
}
