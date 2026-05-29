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

        public static Dictionary<int,KumaPointLight> LightHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("createPoint",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var pos = lua.ToVec3(1);
                var radius = (float)lua.ToNumber(2);
                var color = lua.ToVec3(3);
                var intensity = (float)lua.ToNumber(4);

                lua.PushPointLight(pos,radius,color,intensity);

                return 1;
            })
        ];

        public static void PushPointLight(this Lua lua, Vector3 pos, float radius, Vector3 Color, float Intensity)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);

            LightHandles.Add(handle, new KumaPointLight(pos, radius, Color, Intensity));

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                if (KumaScene.CurrentPointLights.Contains(LightHandles[handle]))
                {
                    KumaScene.CurrentPointLights.Remove(LightHandles[handle]);
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

                if (KumaScene.CurrentPointLights.Contains(LightHandles[handle])) UpdateLights = true;
                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }

        public static KumaPointLight ToPointLight(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (LightHandles.TryGetValue(val, out var light)) return light;
            throw new Exception("Invalid light handle");
        }
    }
}
