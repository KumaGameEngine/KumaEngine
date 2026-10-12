using Assimp;
using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.API
{
    public static class SwapchainAPI
    {
        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("fromName",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var fileName = lua.ToString(1);

                lua.PushSwapchain(fileName);

                return 1;
            }),
        ];

        public static void PushSwapchain(this Lua lua,string swp)
        {
            KumaPass.CheckSwapchain(swp);

            var MODNAME = "swapchain." + swp;

            lua.NewTable();

            lua.PushString(swp);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction("setSizeExpressions", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var w = L.ToString(1);
                var h = L.ToString(2);

                KumaPass.SwapChains[swp].SetSizeExpressions(DefinitionFile.Game.ResourceFactory, w, h);

                return 1;
            });
        }

        public static string ToSwapchain(this Lua lua, int index)
        {
            string val = lua.GetStringField(index,"handle");
            if (KumaPass.SwapChains.ContainsKey(val)) return val;
            throw new Exception("Invalid swapchain handle");
        }
    }
}
