using BulletSharp;
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
    public static class PhysicsAPI
    {
        public static Dictionary<int, CollisionShape> ShapeHandles = new();
        public static Dictionary<int, CollisionObject> ObjectHandles = new();

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("createBoxShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Vector3 size = lua.ToVec3(1);

                lua.PushShape(
                    new BoxShape(size.X / 2, size.Y / 2, size.Z / 2)
                );

                return 1;
            }),
            DefinitionFile.RegisterFunction("createSphereShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                float radius = (float)lua.ToNumber(1);

                lua.PushShape(new SphereShape(radius));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createCapsuleShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                float radius = (float)lua.ToNumber(1);
                float height = (float)lua.ToNumber(2);

                lua.PushShape(new CapsuleShape(radius,height));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createCylinderShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Vector3 size = lua.ToVec3(1);

                lua.PushShape(
                    new CylinderShape(size.X / 2, size.Y / 2, size.Z / 2)
                );

                return 1;
            }),
            DefinitionFile.RegisterFunction("createConeShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                float radius = (float)lua.ToNumber(1);
                float height = (float)lua.ToNumber(2);

                lua.PushShape(new ConeShape(radius,height));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createConvexHullShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Model mdl = lua.ToModel(1);

                lua.PushShape(new ConvexHullShape(mdl.Vertices.Vertices));

                return 1;
            }),
            DefinitionFile.RegisterFunction("createStaticTriangleMeshShape",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Model mdl = lua.ToModel(1);

                Vector3[] vertices = mdl.Vertices.Vertices.ToArray();
                int[] indices = mdl.Indices.Select(x => (int)x).ToArray();

                var indexVertexArray = new TriangleIndexVertexArray(indices, vertices);

                lua.PushShape(
                    new BvhTriangleMeshShape(indexVertexArray, true)
                );

                return 1;
            }),
            DefinitionFile.RegisterFunction("createRigidBody",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var boxShape = lua.ToShape(1);
                float mass = (float)lua.ToNumber(2);
                Vector3 pos = lua.ToVec3(3);
                Vector3 rot = lua.ToVec3(4);
                Vector3 size = lua.ToVec3(5);

                var startTransform = Matrix4x4.CreateScale(size)
                 * Matrix4x4.CreateFromYawPitchRoll(
                     rot.Y * (MathF.PI / 180f),
                     rot.X * (MathF.PI / 180f),
                     rot.Z * (MathF.PI / 180f)
                 ) * Matrix4x4.CreateTranslation(pos);

                Vector3 localInertia = boxShape.CalculateLocalInertia(mass);
                var motionState = new DefaultMotionState(startTransform);
                var rbInfo = new RigidBodyConstructionInfo(mass, motionState, boxShape, localInertia);

                lua.PushRigidBody(new RigidBody(rbInfo));
                return 1;
            }),
        ];

        public static void PushShape(this Lua lua, CollisionShape shape)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);

            ShapeHandles.Add(handle, shape);

            lua.PushInteger(handle);
        }

        public static CollisionShape ToShape(this Lua lua, int index)
        {
            int val = (int)lua.ToInteger(index);
            if (ShapeHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid model handle");
        }

        public static void PushRigidBody(this Lua lua, RigidBody collisonObject)
        {
            var handle = 0;

            if (ObjectHandles.ContainsValue(collisonObject))
                handle = ObjectHandles.First(x => x.Value == collisonObject).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                ObjectHandles.Add(handle, collisonObject);
            }

            var MODNAME = "rigidbody." + handle;

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction("applyForce", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyCentralForce(force);

                return 0;
            });

            lua.PushSafeCFunction("applyImpulse", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyCentralImpulse(force);

                return 0;
            });

            lua.PushSafeCFunction("awake", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                bool forced = L.ToBoolean(1);

                collisonObject.Activate(forced);

                return 0;
            });

            lua.NewTable();

            PushGenericCollisionProperties(lua, handle);

            lua.SetMetaTable(-2);
        }

        public static CollisionObject ToCollisonObject(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (ObjectHandles.TryGetValue(val, out var go)) return go;
            throw new Exception("Invalid GameObject handle");
        }

        static void PushGenericCollisionProperties(Lua lua,int handle)
        {
            var MODNAME = "collision." + handle;

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = ObjectHandles[handle];

                GameObjectAPI.DecomposeTransform(go.WorldTransform, out var pos, out var euler, out var scale);

                switch (key)
                {
                    case "position": L.PushVec3(pos); return 1;
                    case "rotation": L.PushVec3(euler); return 1;
                    case "scale": L.PushVec3(scale); return 1;
                }

                return 1;
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = ObjectHandles[handle];

                GameObjectAPI.DecomposeTransform(go.WorldTransform, out var pos, out var euler, out var scale);

                switch (key)
                {
                    case "position": pos = L.ToVec3(3); break;
                    case "rotation": euler = L.ToVec3(3); break;
                    case "scale": scale = L.ToVec3(3); break;
                }

                go.WorldTransform = GameObjectAPI.ComposeTransform(pos, euler, scale);

                return 0;
            });
        }
    }
}
