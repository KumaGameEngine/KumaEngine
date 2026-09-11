using KeraLua;
using KumaEngine.Rendering;
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
            const string MODNAME = "vector3";

            lua.NewMetaTable(META);

            lua.PushSafeCFunction("__add", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, ToVec3(L, 1) + ToVec3(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__sub", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, ToVec3(L, 1) - ToVec3(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__mul", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                if (L.IsNumber(1))
                    PushVec3(L, ToVec3(L, 2) * (float)L.ToNumber(1));
                else
                    PushVec3(L, ToVec3(L, 1) * (float)L.ToNumber(2));
                return 1;
            });

            lua.PushSafeCFunction("__unm", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec3(L, -ToVec3(L, 1));
                return 1;
            });

            lua.PushSafeCFunction("__eq", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                L.PushBoolean(ToVec3(L, 1) == ToVec3(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__tostring", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var v = ToVec3(L, 1);
                L.PushString($"Vector3({v.X}, {v.Y}, {v.Z})");
                return 1;
            });

            lua.PushCopy(-1);
            lua.SetField(-2, "__index");

            lua.Pop(1);
        }

        public static void PushVec3(this Lua lua, Vector3 vec)
        {
            lua.NewTable();

            lua.PushNumber(vec.X);
            lua.SetField(-2, "x");

            lua.PushNumber(vec.Y);
            lua.SetField(-2, "y");

            lua.PushNumber(vec.Z);
            lua.SetField(-2, "z");

            lua.GetMetaTable(META);
            lua.SetMetaTable(-2);
        }

        public static Vector3 ToVec3(this Lua lua, int idx)
        {
            return new Vector3(
                (float)lua.GetNumField(idx, "x"), 
                (float)lua.GetNumField(idx, "y"), 
                (float)lua.GetNumField(idx, "z")
            );
        }
    }
}