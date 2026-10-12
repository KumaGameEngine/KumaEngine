using Assimp;
using BulletSharp;
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
    public static class CameraAPI
    {
        public static Dictionary<int,Camera> CameraHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("create",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);

                var camera = new Camera(
                    DefinitionFile.Game.ResourceFactory
                );

                lua.PushCamera(camera);
                
                return 1;
            }),
        ];

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

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

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

            lua.PushSafeCFunction("unprojectVector", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 ws = L.ToVec3(1);

                L.PushVec3(camera.UnprojectVector(ws));

                return 1;
            });

            lua.PushSafeCFunction("screenToNdc", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 ws = L.ToVec3(1);

                L.PushVec3(camera.ScreenToNdc(ws));

                return 1;
            });

            lua.PushSafeCFunction("ndcToScreen", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 ws = L.ToVec3(1);

                L.PushVec3(camera.NdcToScreen(ws));

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
                        L.PushVec3(camera.Right);
                        return 1;
                    case "up":
                        L.PushVec3(camera.Up);
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
                    case "targetSwapchain":
                        L.PushSwapchain(camera.DefaultSwapchain);
                        return 1;
                    case "clearMode":
                        L.PushInteger((int)camera.ClearMode);
                        return 1;
                    case "enabled":
                        L.PushBoolean(camera.Enabled);
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
                    case "targetSwapchain":
                        camera.DefaultSwapchain = L.ToSwapchain(3);
                        break;
                    case "clearMode":
                        camera.ClearMode = (CameraClearMode)L.ToInteger(3);
                        break;
                    case "enabled":
                        camera.Enabled = L.ToBoolean(3);
                        break;
                }
                return 0;
            });

            lua.SetMetaTable(-2);
        }
        public static Camera ToCamera(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (CameraHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid camera handle");
        }

        public static void PushCameraList(this Lua lua, string MODNAME, List<Camera> list) =>
            lua.PushList(MODNAME, list, (l, v) => l.PushCamera(v), (l, _) => l.ToCamera(3));
    }
}
