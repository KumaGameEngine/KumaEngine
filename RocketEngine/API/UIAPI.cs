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

                lua.PushUISurface(d,f);

                return 1;
            })
        ];

        public static void PushElement(this Lua lua,int handle, string id)
        {
            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string p = L.ToString(1);
                string v = L.ToString(2);

                UISurfaceHandles[handle].GetElementById(id)!.SetAttribute(p, v);

                return 0;
            });
            lua.SetField(-2, "setAttribute");
        }

        public static void PushUISurface(this Lua lua, string name,string file)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);
            UISurfaceHandles.Add(handle, new UISurface(name, file));

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                UISurfaceHandles[handle].Dispose();
                UISurfaceHandles.Remove(handle);
                return 0;
            });
            lua.SetField(-2, "destroy");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var rml = L.ToString(1);
                UISurfaceHandles[handle].LoadDocument(rml);
                return 0;
            });
            lua.SetField(-2, "loadRml");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var id = lua.ToString(1);

                lua.PushElement(handle, id);
                return 1;
            });
            lua.SetField(-2, "getElementById");

            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                switch (key)
                {
                    case "visible": L.PushBoolean(UISurfaceHandles[handle].Visible); return 1;
                    default: L.PushNil(); return 1;
                }
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                switch (key)
                {
                    case "visible": UISurfaceHandles[handle].Visible = L.ToBoolean(3); break;
                }

                return 0;
            });
            lua.SetField(-2, "__newindex");

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
