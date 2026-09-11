using Assimp;
using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Camera = KumaEngine.Rendering.Camera;

namespace KumaEngine.API
{
    public static class SceneAPI
    {
        public static Dictionary<int,KumaScene> SceneHandles = new();
        public static Dictionary<int,Camera> CameraHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("create",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                var scene = new KumaScene(
                    new Camera(
                        DefinitionFile.Game.MainSwapchain.Framebuffer.Width,
                        DefinitionFile.Game.MainSwapchain.Framebuffer.Height,
                        DefinitionFile.Game.ResourceFactory
                    )
                );

                lua.PushScene(scene);

                return 1;
            }),
            DefinitionFile.RegisterFunction("getCamera",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                lua.PushCamera(KumaScene.CurrentScene.Camera);

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

        public static void PushScene(this Lua lua, KumaScene scene)
        {
            var handle = 0;

            if (SceneHandles.ContainsValue(scene))
                handle = SceneHandles.First(x => x.Value == scene).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                SceneHandles.Add(handle, scene);
            }

            var MODNAME = "scene." + handle;

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2,"handle");

            lua.PushSafeCFunction("appendChild", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToGameObject(1);

                void iteradd(GameObject o)
                {
                    foreach (var item in GameObjectAPI.GameObjectHandles.Where(x=>
                        x.Value.Parent == o && 
                        !SceneHandles[handle].GameObjects.Contains(o)
                    ))
                    {
                        SceneHandles[handle].GameObjects.Add(item.Value);
                        iteradd(item.Value);
                    }
                }

                SceneHandles[handle].GameObjects.Add(go);
                iteradd(go);

                return 1;
            });

            lua.PushSafeCFunction("removeChild", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToGameObject(1);

                void iteradd(GameObject o)
                {
                    foreach (var item in GameObjectAPI.GameObjectHandles.Where(x =>
                        x.Value.Parent == o &&
                        !SceneHandles[handle].GameObjects.Contains(o)
                    ))
                    {
                        SceneHandles[handle].GameObjects.Remove(item.Value);
                        iteradd(item.Value);
                    }
                }

                SceneHandles[handle].GameObjects.Remove(go);
                iteradd(go);

                return 1;
            });

            lua.PushSafeCFunction("appendLight", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToLight(1);

                SceneHandles[handle].Lights.Add(go);

                return 1;
            });

            lua.PushSafeCFunction("removeLight", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToLight(1);

                SceneHandles[handle].Lights.Remove(go);

                return 1;
            });

            lua.PushSafeCFunction("appendPhysicBody", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToCollisonObject(1);

                SceneHandles[handle].World.AddCollisionObject(go);

                return 1;
            });

            lua.PushSafeCFunction("removePhysicBody", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToCollisonObject(1);

                SceneHandles[handle].World.RemoveCollisionObject(go);

                return 1;
            });

            lua.PushSafeCFunction("setSkybox", MODNAME, ilua =>
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

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);
                switch (key)
                {
                    case "camera":
                        L.PushCamera(SceneHandles[handle].Camera);
                        return 1;
                    case "sun":
                        L.PushSun(SceneHandles[handle].Sun);
                        return 1;
                }

                var go = SceneHandles[handle].GameObjects.FirstOrDefault(x =>
                    x.Parent == null! &&
                    x.Name == key,
                    null!
                );

                L.PushGameObject(go);

                return 1;
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);
                switch (key)
                {
                    case "sun":
                        SceneHandles[handle].Sun = L.ToSun(3);
                        break;
                }
                return 0;
            });

            lua.SetMetaTable(-2);
        }

        public static KumaScene ToScene(this Lua lua,int index)
        {
            int val = lua.GetIntField(index,"handle");
            if (SceneHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid scene handle");
        }

        public static void PushCamera(this Lua lua, Camera camera)
        {
            var handle = 0;

            if (CameraHandles.ContainsValue(camera))
                handle = CameraHandles.First(x => x.Value == camera).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                CameraHandles.Add(handle, camera);
            }

            var MODNAME = "camera." + handle;

            lua.NewTable();

            lua.PushSafeCFunction("lookAt", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 rot = L.ToVec3(1);

                camera.LookAt(rot);

                return 0;
            });

            lua.PushSafeCFunction("projectVector", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 ws = L.ToVec3(1);

                L.PushVec3(camera.ProjectVector(ws));

                return 1;
            });

            lua.PushSafeCFunction("getUnitsPerPixel", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                float plane = (float)L.ToNumber(1);

                L.PushVec3(new(camera.GetUnitsPerPixel(plane),0));

                return 1;
            });

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
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
                    case "orthographic":
                        L.PushBoolean(camera.Orthographic);
                        return 1;
                    case "orthoSize":
                        L.PushNumber(camera.OrthographicSize);
                        return 1;
                    case "fov":
                        L.PushNumber(camera.FieldOfView * (180f / MathF.PI));
                        return 1;
                    default:
                        L.PushNil();
                        return 1;
                }
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
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
                    case "orthographic":
                        camera.Orthographic = L.ToBoolean(3);
                        break;
                    case "orthoSize":
                        camera.OrthographicSize = (float)L.ToNumber(3);
                        break;
                    case "fov":
                        camera.FieldOfView = (float)L.ToNumber(3) * (MathF.PI / 180f);
                        break;
                }
                return 0;
            });

            lua.SetMetaTable(-2);
        }
    }
}
