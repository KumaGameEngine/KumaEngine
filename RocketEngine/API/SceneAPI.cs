using Assimp;
using BulletSharp;
using KeraLua;
using KumaEngine.Physics;
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

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("create",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                var scene = new KumaScene(
                    new Camera(
                        DefinitionFile.Game.ResourceFactory
                    ) { Enabled = true }
                );

                lua.PushScene(scene);

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

            lua.PushCameraList(MODNAME + ".cameras", SceneHandles[handle].Cameras);
            lua.SetField(-2, "cameras");

            lua.PushSafeCFunction("appendChild", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToGameObject(1);

                void iteradd(GameObject o)
                {
                    foreach (var item in GameObjectAPI.GameObjectHandles.Where(x=>
                        x.Value.Parent == o && 
                        !SceneHandles[handle].GameObjects.Contains(x.Value)
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

                if (go is TriggerCollision tc)
                {
                    SceneHandles[handle].World.AddCollisionObject(
                        go,
                        CollisionFilterGroups.SensorTrigger,
                        tc.Group
                    );

                    SceneHandles[handle].ActiveTriggers.Add(tc);
                }
                else SceneHandles[handle].World.AddCollisionObject(go);

                return 1;
            });

            lua.PushSafeCFunction("removePhysicBody", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var go = lua.ToCollisonObject(1);

                if (go is TriggerCollision tc)
                    SceneHandles[handle].ActiveTriggers.Remove(tc);

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

            lua.PushSafeCFunction("raycast", MODNAME, ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Vector3 origin = lua.ToVec3(1);
                Vector3 rotation = lua.ToVec3(2);
                float lenght = (int)lua.ToNumber(3);

                var direction = Camera.EulerToDirection(rotation);

                var to = origin + (direction * lenght);

                ClosestRayResultCallback callback = new(ref origin,ref to);

                SceneHandles[handle].World.RayTest(origin,to,callback);

                if (callback.HasHit) lua.PushHit(
                    callback.HitPointWorld,
                    callback.HitNormalWorld,
                    callback.CollisionObject
                );
                else lua.PushNil();

                return 1;
            });

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);
                switch (key)
                {
                    case "sun": L.PushSun(SceneHandles[handle].Sun); return 1;
                    case "gravity": L.PushVec3(SceneHandles[handle].Gravity); return 1;
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
                    case "sun": SceneHandles[handle].Sun = L.ToSun(3); break;
                    case "gravity": SceneHandles[handle].Gravity = L.ToVec3(3); break;
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

        public static void PushHit(this Lua lua, Vector3 point, Vector3 normal, CollisionObject collision)
        {
            lua.NewTable();

            lua.PushVec3(point);
            lua.SetField(-2, "position");

            lua.PushVec3(normal);
            lua.SetField(-2, "normal");

            if (collision is RigidBody rb) lua.PushRigidBody(rb);
            else lua.PushNil();
            lua.SetField(-2, "object");
        }
    }
}
