using KeraLua;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KumaEngine.API
{
    public static class Vector3API
    {
        const string META = "vector3";

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("new", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                float x = (float)lua.ToNumber(1);
                float y = (float)lua.ToNumber(2);
                float z = (float)lua.ToNumber(3);
                PushVec3(lua, new Vector3(x, y, z));
                return 1;
            }),
            DefinitionFile.RegisterFunction("dot", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var a = ToVec3(lua, 1);
                var b = ToVec3(lua, 2);
                lua.PushNumber(Vector3.Dot(a, b));
                return 1;
            }),
            DefinitionFile.RegisterFunction("cross", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var a = ToVec3(lua, 1);
                var b = ToVec3(lua, 2);
                PushVec3(lua, Vector3.Cross(a, b));
                return 1;
            }),
            DefinitionFile.RegisterFunction("length", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                lua.PushNumber(ToVec3(lua, 1).Length());
                return 1;
            }),
            DefinitionFile.RegisterFunction("normalize", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                PushVec3(lua, Vector3.Normalize(ToVec3(lua, 1)));
                return 1;
            }),
        ];

        public static void RegisterMeta(Lua lua)
        {
            lua.NewMetaTable(META);

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, ToVec3(L, 1) + ToVec3(L, 2));
                return 1;
            });
            lua.SetField(-2, "__add");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, ToVec3(L, 1) - ToVec3(L, 2));
                return 1;
            });
            lua.SetField(-2, "__sub");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                if (L.IsNumber(1))
                    PushVec3(L, ToVec3(L, 2) * (float)L.ToNumber(1));
                else
                    PushVec3(L, ToVec3(L, 1) * (float)L.ToNumber(2));
                return 1;
            });
            lua.SetField(-2, "__mul");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, -ToVec3(L, 1));
                return 1;
            });
            lua.SetField(-2, "__unm");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                L.PushBoolean(ToVec3(L, 1) == ToVec3(L, 2));
                return 1;
            });
            lua.SetField(-2, "__eq");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var v = ToVec3(L, 1);
                L.PushString($"Vector3({v.X}, {v.Y}, {v.Z})");
                return 1;
            });
            lua.SetField(-2, "__tostring");

            lua.PushCopy(-1);
            lua.SetField(-2, "__index");

            lua.Pop(1);
        }

        public static void PushVec3(this Lua lua, Vector3 vec)
        {
            unsafe
            {
                var ptr = (Vector3*)lua.NewUserData(sizeof(Vector3));
                *ptr = vec;
            }
            lua.GetMetaTable(META);
            lua.SetMetaTable(-2);
        }

        public static Vector3 ToVec3(this Lua lua, int idx)
        {
            unsafe
            {
                var ptr = (Vector3*)lua.ToUserData(idx);
                return *ptr;
            }
        }
    }
}