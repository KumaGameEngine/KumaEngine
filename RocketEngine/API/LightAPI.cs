using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace KumaEngine.API
{
    public static class LightAPI
    {
        public static bool UpdateLights { get; set; }

        public static Dictionary<int,KumaLight> LightHandles = new();
        public static Dictionary<int,KumaSun> SunHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("createPoint",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var pos = lua.ToVec3(1);
                var radius = (float)lua.ToNumber(2);
                var color = lua.ToVec3(3);
                var intensity = (float)lua.ToNumber(4);

                lua.PushLight(new(pos,radius,color,intensity));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createSun",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var direction = lua.ToVec3(1);
                var color = lua.ToVec3(2);
                var intensity = (float)lua.ToNumber(3);

                lua.PushSun(new(direction,color,intensity));

                return 1;
            })
        ];

        public static void PushLight(this Lua lua, KumaLight light)
        {
            var handle = 0;

            if (LightHandles.ContainsValue(light))
                handle = LightHandles.First(x => x.Value == light).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                LightHandles.Add(handle, light);
            }

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                if (KumaScene.CurrentLights.Contains(LightHandles[handle]))
                {
                    KumaScene.CurrentLights.Remove(LightHandles[handle]);
                    UpdateLights = true;
                }

                LightHandles.Remove(handle);

                return 0;
            });
            lua.SetField(-2, "destroy");

            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (!LightHandles.TryGetValue(handle, out var light))
                {
                    L.PushNil();
                    return 1;
                }

                switch (key)
                {
                    case "position": L.PushVec3(light.Position); return 1;
                    case "radius": L.PushNumber(light.Radius); return 1;
                    case "color": L.PushVec3(light.Color); return 1;
                    case "intensity": L.PushNumber(light.Intensity); return 1;
                    default: L.PushNil(); return 1;
                }
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (!LightHandles.TryGetValue(handle, out var light)) return 0;

                switch (key)
                {
                    case "position": light.Position = L.ToVec3(3); break;
                    case "radius": light.Radius = (float)L.ToNumber(3); break;
                    case "color": light.Color = L.ToVec3(3); break;
                    case "intensity": light.Intensity = (float)L.ToNumber(3); break;
                }

                if (KumaScene.CurrentLights.Contains(LightHandles[handle])) UpdateLights = true;
                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }

        public static KumaLight ToLight(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (LightHandles.TryGetValue(val, out var light)) return light;
            throw new Exception("Invalid light handle");
        }

        public static void PushSun(this Lua lua, KumaSun light)
        {
            var handle = 0;

            if (SunHandles.ContainsValue(light))
                handle = SunHandles.First(x => x.Value == light).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                SunHandles.Add(handle, light);
            }

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                SunHandles.Remove(handle);

                return 0;
            });
            lua.SetField(-2, "destroy");

            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (!SunHandles.TryGetValue(handle, out var light))
                {
                    L.PushNil();
                    return 1;
                }

                switch (key)
                {
                    case "direction": L.PushVec3(light.Direction); return 1;
                    case "color": L.PushVec3(light.Color); return 1;
                    case "intensity": L.PushNumber(light.Intensity); return 1;
                    default: L.PushNil(); return 1;
                }
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                if (!SunHandles.TryGetValue(handle, out var light)) return 0;

                switch (key)
                {
                    case "direction": light.Direction = L.ToVec3(3); break;
                    case "color": light.Color = L.ToVec3(3); break;
                    case "intensity": light.Intensity = (float)L.ToNumber(3); break;
                }

                if (KumaScene.CurrentSun == SunHandles[handle]) UpdateLights = true;
                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }

        public static KumaSun ToSun(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (SunHandles.TryGetValue(val, out var light)) return light;
            throw new Exception("Invalid light handle");
        }
    }
}
