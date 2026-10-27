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
using TriangleMeshShape = KumaEngine.Physics.TriangleMeshShape;

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
                bool quantize = lua.ToBoolean(2);

                int[] indices = mdl.Indices.Select(x => (int)x).ToArray();

                lua.PushShape(
                    TriangleMeshShape.CreateFromTriangleList(
                        mdl.Vertices.Vertices,
                        indices, quantize
                    )
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
            DefinitionFile.RegisterFunction("createTrigger",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                Vector3 size = lua.ToVec3(1);
                Vector3 position = lua.ToVec3(2);
                Vector3 rotation = lua.ToVec3(3);
                Vector3 scale = lua.ToVec3(4);
                CollisionFilterGroups filter = (CollisionFilterGroups)lua.ToInteger(5);

                lua.PushTrigger(new(size / 2, position, rotation, scale, filter));

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

        public static void PushTrigger(this Lua lua, TriggerCollision collisonObject)
        {
            var handle = 0;

            if (ObjectHandles.ContainsValue(collisonObject))
                handle = ObjectHandles.First(x => x.Value == collisonObject).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                ObjectHandles.Add(handle, collisonObject);
            }

            var MODNAME = "trigger." + handle;

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");   

            lua.PushSafeCFunction("bindOnEnter", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var fn = L.GetFunction(1);

                collisonObject.OnEnter += obj =>
                {
                    if (obj is RigidBody rb)
                    {
                        DefinitionFile.lua.RawGetInteger(LuaRegistry.Index, fn);

                        DefinitionFile.lua.PushRigidBody(rb);
                        DefinitionFile.lua.PCall(1, 0, 0);
                    }
                };

                return 0;
            });

            lua.PushSafeCFunction("bindOnExit", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var fn = L.GetFunction(1);

                collisonObject.OnExit += obj =>
                {
                    if (obj is RigidBody rb)
                    {
                        DefinitionFile.lua.RawGetInteger(LuaRegistry.Index, fn);

                        DefinitionFile.lua.PushRigidBody(rb);
                        DefinitionFile.lua.PCall(1, 0, 0);
                    }
                };

                return 0;
            });

            lua.PushSafeCFunction("bindOnStep", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var fn = L.GetFunction(1);

                collisonObject.OnTick += obj =>
                {
                    if (obj is RigidBody rb)
                    {
                        DefinitionFile.lua.RawGetInteger(LuaRegistry.Index, fn);

                        DefinitionFile.lua.PushRigidBody(rb);
                        DefinitionFile.lua.PCall(1, 0, 0);
                    }
                };

                return 0;
            });

            lua.PushSafeCFunction("dispose", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                bool forced = L.ToBoolean(1);

                collisonObject.Dispose();

                return 0;
            });

            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = (ObjectHandles[handle] as TriggerCollision)!;

                GameObjectAPI.DecomposeTransform(go.WorldTransform, out var pos, out var euler, out var scale);

                switch (key)
                {
                    case "position": L.PushVec3(pos); return 1;
                    case "rotation": L.PushVec3(euler); return 1;
                    case "scale": L.PushVec3(scale); return 1;
                }

                L.PushNil();
                return 1;
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = (ObjectHandles[handle] as TriggerCollision)!;

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

            lua.SetMetaTable(-2);
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
                Vector3 pos = L.ToVec3(2);

                collisonObject.ApplyForce(force, pos);

                return 0;
            });

            lua.PushSafeCFunction("applyImpulse", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);
                Vector3 pos = L.ToVec3(2);

                collisonObject.ApplyImpulse(force, pos);

                return 0;
            });

            lua.PushSafeCFunction("applyTourque", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyTorque(force);

                return 0;
            });

            lua.PushSafeCFunction("applyTourqueImpulse", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyTorque(force);

                return 0;
            });

            lua.PushSafeCFunction("applyCentralForce", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyCentralForce(force);

                return 0;
            });

            lua.PushSafeCFunction("applyCentralImpulse", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.ApplyCentralImpulse(force);

                return 0;
            });

            lua.PushSafeCFunction("getVelocityAtPoint", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 force = L.ToVec3(1);

                collisonObject.GetVelocityInLocalPoint(ref force, out var vel);

                L.PushVec3(vel);
                return 1;
            });

            lua.PushSafeCFunction("teleport", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 pos = L.ToVec3(1);
                Vector3 euler = L.ToVec3(2);
                Vector3 scale = L.ToVec3(3);

                collisonObject.Activate(true);

                collisonObject.WorldTransform = GameObjectAPI.ComposeTransform(pos, euler, scale);

                return 0;
            });

            lua.PushSafeCFunction("ignoreCollisions", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                CollisionObject obj = L.ToCollisonObject(1);
                bool ignore = L.ToBoolean(2);

                collisonObject.SetIgnoreCollisionCheck(obj,ignore);
                obj.SetIgnoreCollisionCheck(collisonObject,ignore);

                return 0;
            });

            lua.PushSafeCFunction("ClearForces", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);

                collisonObject.ClearForces();

                return 0;
            });

            lua.PushSafeCFunction("ClearGravity", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                
                collisonObject.ClearGravity();

                return 0;
            });

            lua.PushSafeCFunction("awake", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                bool forced = L.ToBoolean(1);

                collisonObject.Activate(forced);

                return 0;
            });

            lua.PushSafeCFunction("forceActive", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                bool forced = L.ToBoolean(1);

                collisonObject.ForceActivationState(forced ? ActivationState.DisableDeactivation : ActivationState.Undefined);

                return 0;
            });

            lua.PushSafeCFunction("dispose", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                bool forced = L.ToBoolean(1);

                collisonObject.Dispose();

                return 0;
            });

            lua.NewTable();

            PushRigidBodyProperties(lua, handle);

            lua.SetMetaTable(-2);
        }

        public static CollisionObject ToCollisonObject(this Lua lua, int index)
        {
            int val = lua.GetIntField(index, "handle");
            if (ObjectHandles.TryGetValue(val, out var go)) return go;
            throw new Exception("Invalid CollisionObject handle");
        }

        static void PushRigidBodyProperties(Lua lua,int handle)
        {
            var MODNAME = "collision." + handle;

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = (ObjectHandles[handle] as RigidBody)!;

                switch (key)
                {
                    case "friction": L.PushNumber(go.Friction); return 1;
                    case "rollingFriction": L.PushNumber(go.RollingFriction); return 1;
                    case "anisotropicFriction": L.PushVec3(go.AnisotropicFriction); return 1;
                    case "linearVelocity": L.PushVec3(go.LinearVelocity); return 1;
                    case "angularVelocity": L.PushVec3(go.AngularVelocity); return 1;
                    case "linearDampening": L.PushNumber(go.LinearDamping); return 1;
                    case "angularDampening": L.PushNumber(go.AngularDamping); return 1;
                    case "contactDampening": L.PushNumber(go.ContactDamping); return 1;
                    case "linearFactor": L.PushVec3(go.LinearFactor); return 1;
                    case "angularFactor": L.PushVec3(go.AngularFactor); return 1;
                    case "centerOfMass": L.PushVec3(go.CenterOfMassPosition); return 1;
                    case "collisonGroup": L.PushInteger(go.BroadphaseHandle.CollisionFilterGroup); return 1;
                    case "collisonMask": L.PushInteger(go.BroadphaseHandle.CollisionFilterMask); return 1;
                    case "collisonFlags": L.PushInteger((int)go.CollisionFlags); return 1;
                }

                GameObjectAPI.DecomposeTransform(go.WorldTransform, out var pos, out var euler, out var scale);

                switch (key)
                {
                    case "position": L.PushVec3(pos); return 1;
                    case "rotation": L.PushVec3(euler); return 1;
                    case "scale": L.PushVec3(scale); return 1;
                }

                L.PushNil();
                return 1;
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string key = L.ToString(2);

                var go = (ObjectHandles[handle] as RigidBody)!;

                switch (key)
                {
                    case "friction": go.Friction = (float)L.ToNumber(3); return 0;
                    case "rollingFriction": go.RollingFriction = (float)L.ToNumber(3); return 0;
                    case "anisotropicFriction": go.AnisotropicFriction = L.ToVec3(3); return 0;
                    case "linearVelocity": go.LinearVelocity = L.ToVec3(3); return 0;
                    case "angularVelocity": go.AngularVelocity = L.ToVec3(3); return 0;
                    case "linearFactor": go.LinearFactor = L.ToVec3(3); return 0;
                    case "angularFactor": go.AngularFactor = L.ToVec3(3); return 0;
                    case "collisonGroup": go.BroadphaseHandle.CollisionFilterGroup = (int)L.ToInteger(3); return 0;
                    case "collisonMask": go.BroadphaseHandle.CollisionFilterMask = (int)L.ToInteger(3); return 0;
                    case "collisonFlags": go.CollisionFlags = (CollisionFlags)L.ToInteger(3); return 0;
                }

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
