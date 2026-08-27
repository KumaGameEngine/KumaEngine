using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace KumaEngine.API
{
    public static class GameObjectAPI
    {
        public static Dictionary<int, GameObject> GameObjectHandles = new();

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("create", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var name = lua.ToString(1);
                var mdl = lua.ToModel(2);
                var pipeline = lua.ToPipeline(3);
                lua.PushGameObject(new GameObject(name, pipeline, mdl));
                return 1;
            }),
            DefinitionFile.RegisterFunction("createEmpty", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var name = lua.ToString(1);
                lua.PushGameObject(new GameObject(name, null!, null!));
                return 1;
            })
        ];

        public static void DecomposeTransform(Matrix4x4 m,
            out Vector3 position, out Vector3 eulerAngles, out Vector3 scale)
        {
            Matrix4x4.Decompose(m, out scale, out Quaternion rot, out position);
            eulerAngles = QuaternionToEuler(rot);
        }

        public static Matrix4x4 ComposeTransform(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale)
                 * Matrix4x4.CreateFromQuaternion(EulerToQuaternion(eulerAngles))
                 * Matrix4x4.CreateTranslation(position);
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            float sinPitchCosPitch = 2f * (q.W * q.X + q.Y * q.Z);
            float cosPitchCosPitch = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            float pitch = MathF.Atan2(sinPitchCosPitch, cosPitchCosPitch);

            float sinYaw = 2f * (q.W * q.Y - q.Z * q.X);
            float yaw = MathF.Abs(sinYaw) >= 1f
                ? MathF.CopySign(MathF.PI / 2f, sinYaw)
                : MathF.Asin(sinYaw);

            float sinRollCosPitch = 2f * (q.W * q.Z + q.X * q.Y);
            float cosRollCosPitch = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            float roll = MathF.Atan2(sinRollCosPitch, cosRollCosPitch);

            const float toDeg = 180f / MathF.PI;
            return new Vector3(pitch * toDeg, yaw * toDeg, roll * toDeg);
        }

        public static Quaternion EulerToQuaternion(Vector3 eulerDegrees)
        {
            const float toRad = MathF.PI / 180f;
            return Quaternion.CreateFromYawPitchRoll(
                eulerDegrees.Y * toRad,
                eulerDegrees.X * toRad,
                eulerDegrees.Z * toRad
            );
        }

        public static void PushGameObject(this Lua lua, GameObject gameobject)
        {
            var handle = 0;

            if (GameObjectHandles.ContainsValue(gameobject))
                handle = GameObjectHandles.First(x => x.Value == gameobject).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                GameObjectHandles.Add(handle, gameobject);
            }
            
            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                GameObjectHandles[handle].Destroy();
                GameObjectHandles.Remove(handle);
                return 0;
            });
            lua.SetField(-2, "destroy");

            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                DecomposeTransform(GameObjectHandles[handle].Transform,
                    out var pos, out var rot, out var scl);

                switch (key)
                {
                    case "position": L.PushVec3(pos); return 1;
                    case "rotation": L.PushVec3(rot); return 1;
                    case "scale": L.PushVec3(scl); return 1;
                    case "model": L.PushModel(GameObjectHandles[handle].Model); return 1;
                    case "material": L.PushMaterial(GameObjectHandles[handle].Material); return 1;
                    case "name": L.PushString(GameObjectHandles[handle].Name); return 1;
                    case "parent": L.PushGameObject(GameObjectHandles[handle].Parent); return 1;
                }

                var go = GameObjectHandles.Values.FirstOrDefault(x => 
                    x.Parent == GameObjectHandles[handle] && 
                    x.Name == key, 
                    null!
                );

                L.PushGameObject(go);

                return 1;
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                DecomposeTransform(GameObjectHandles[handle].Transform,
                    out var pos, out var rot, out var scl);

                switch (key)
                {
                    case "position": pos = L.ToVec3(3); break;
                    case "rotation": rot = L.ToVec3(3); break;
                    case "scale": scl = L.ToVec3(3); break;
                    case "model": GameObjectHandles[handle].Model = L.ToModel(3); break;
                    case "material": GameObjectHandles[handle].Material = L.ToMaterial(3); break;
                    case "name": GameObjectHandles[handle].Name = L.ToString(3); break;
                    case "parent": GameObjectHandles[handle].Parent = L.ToGameObject(3); break;
                }

                GameObjectHandles[handle].Transform = ComposeTransform(pos, rot, scl);
                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }

        public static GameObject ToGameObject(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (GameObjectHandles.TryGetValue(val, out var go)) return go;
            throw new Exception("Invalid GameObject handle");
        }
    }
}