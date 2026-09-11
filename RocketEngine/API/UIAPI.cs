using Assimp;
using KeraLua;
using KumaEngine.Rendering;
using KumaEngine.UI;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vulkan;
using Vulkan.Xlib;

namespace KumaEngine.API
{
    public static class UIAPI
    {
        public static Dictionary<int, UISurface> UISurfaceHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("createSurface",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var d = lua.ToString(1);
                var f = lua.ToString(2);

                lua.PushUISurface(new UISurface(d, f));

                return 1;
            })
        ];

        public static void PushElement(this Lua lua,int handle, string id)
        {
            var MODNAME = "element." + handle;

            lua.NewTable();

            lua.PushSafeCFunction("setAttribute", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string p = L.ToString(1);
                string v = L.ToString(2);

                UISurfaceHandles[handle].GetElementById(id)!.SetAttribute(p, v);

                return 0;
            });
        }

        public static void PushUISurface(this Lua lua, UISurface surface)
        {
            var handle = 0;

            if (UISurfaceHandles.ContainsValue(surface))
                handle = UISurfaceHandles.First(x => x.Value == surface).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                UISurfaceHandles.Add(handle, surface);
            }

            var MODNAME = "surface." + handle;

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction("destroy", MODNAME, _ =>
            {
                UISurfaceHandles[handle].Dispose();
                UISurfaceHandles.Remove(handle);
                return 0;
            });

            lua.PushSafeCFunction("loadRml", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var rml = L.ToString(1);
                UISurfaceHandles[handle].LoadDocument(rml);
                return 0;
            });

            lua.PushSafeCFunction("getElementById", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var id = lua.ToString(1);

                lua.PushElement(handle, id);
                return 1;
            });

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                switch (key)
                {
                    case "visible": L.PushBoolean(UISurfaceHandles[handle].Visible); return 1;
                    default: L.PushNil(); return 1;
                }
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                switch (key)
                {
                    case "visible": UISurfaceHandles[handle].Visible = L.ToBoolean(3); break;
                }

                return 0;
            });

            lua.SetMetaTable(-2);
        }

        public static UISurface ToUISurface(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (UISurfaceHandles.TryGetValue(val, out var go)) return go;
            throw new Exception("Invalid UISurface handle");
        }
    }
}
