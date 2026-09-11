using KeraLua;
using KumaEngine.Input;
using KumaEngine.Rendering;
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
        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("cursorLocked",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var b = lua.ToBoolean(1);

                DefinitionFile.Game.Window.HideCursor(b);
                DefinitionFile.Game.Window.RelativeCursor(b);

                return 1;
            }),
            DefinitionFile.RegisterFunction("isKeyDown",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToInteger(1);

                lua.PushBoolean(InputTracker.GetKey((Key)key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("isMouseDown",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var key = lua.ToInteger(1);

                lua.PushBoolean(InputTracker.GetMouseButton((MouseButton)key));

                return 1;
            }),
            DefinitionFile.RegisterFunction("getMousePosition",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushVec3(new(InputTracker.MousePosition,0));

                return 1;
            }),
            DefinitionFile.RegisterFunction("getScrollDelta",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushNumber(InputTracker.ScrollDelta);

                return 1;
            }),
            DefinitionFile.RegisterFunction("getMousePositionDelta",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushVec3(new(DefinitionFile.Game.Window.MouseDelta(),0));

                return 1;
            })
        ];
    }
}
