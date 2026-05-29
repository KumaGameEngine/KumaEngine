using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.API
{
    public static class SceneAPI
    {
        public static Dictionary<int,KumaScene> SceneHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("create",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushScene();

                return 1;
            }),
            DefinitionFile.RegisterFunction("getCamera",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushCamera(DefinitionFile.Game.Camera);

                return 1;
            }),
            DefinitionFile.RegisterFunction("switchScene",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var scene = lua.ToScene(1);

                KumaScene.SwitchScene(
                    scene,
                    DefinitionFile.Game.GraphicsDevice,
                    DefinitionFile.Game.ResourceFactory
                );

                LightAPI.UpdateLights = true;

                return 1;
            })
        ];

        public static void PushScene(this Lua lua)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);

            SceneHandles.Add(handle, new KumaScene());

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2,"handle");

            lua.PushSafeCFunction(ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToGameObject(1);

                SceneHandles[handle].GameObjects.Add(go);

                return 1;
            });
            lua.SetField(-2, "appendChild");

            lua.PushSafeCFunction(ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToPointLight(1);

                SceneHandles[handle].PointLights.Add(go);

                return 1;
            });
            lua.SetField(-2, "appendLight");

            lua.PushSafeCFunction(ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var skyfile = lua.ToString(1);

                SceneHandles[handle].SkyboxMat = KumaMaterial.FromFile(
                    DefinitionFile.Game.GraphicsDevice, 
                    DefinitionFile.Game.ResourceFactory,
                    skyfile
                );

                return 1;
            });
            lua.SetField(-2, "setSkybox");
        }

        public static KumaScene ToScene(this Lua lua,int index)
        {
            int val = lua.GetIntField(index,"handle");
            if (SceneHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid scene handle");
        }

        public static void PushCamera(this Lua lua, Camera camera)
        {
            lua.NewTable();

            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);
                switch (key)
                {
                    case "position":
                        L.PushVec3(camera.Position);
                        return 1;
                    case "rotation":
                        L.PushVec3(camera.Rotation);
                        return 1;
                    case "forward":
                        L.PushVec3(camera.Forward);
                        return 1;
                    case "right":
                        L.PushVec3(camera.right);
                        return 1;
                    default:
                        L.PushNil();
                        return 1;
                }
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);
                switch (key)
                {
                    case "position":
                        camera.Position = L.ToVec3(3);
                        break;
                    case "rotation":
                        camera.Rotation = L.ToVec3(3);
                        break;
                }
                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }
    }
}
