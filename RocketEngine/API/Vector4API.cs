using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KumaEngine.API
{
    public static class Vector4API
    {
        const string META = "vector4";

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("new", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                float x = (float)lua.ToNumber(1);
                float y = (float)lua.ToNumber(2);
                float z = (float)lua.ToNumber(3);
                float w = (float)lua.ToNumber(4);
                PushVec4(lua, new Vector4(x, y, z, w));
                return 1;
            }),
            DefinitionFile.RegisterFunction("dot", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var a = ToVec4(lua, 1);
                var b = ToVec4(lua, 2);
                lua.PushNumber(Vector4.Dot(a, b));
                return 1;
            }),
            DefinitionFile.RegisterFunction("length", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                lua.PushNumber(ToVec4(lua, 1).Length());
                return 1;
            }),
            DefinitionFile.RegisterFunction("normalize", ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                PushVec4(lua, Vector4.Normalize(ToVec4(lua, 1)));
                return 1;
            }),
        ];

        public static void RegisterMeta(Lua lua)
        {
            const string MODNAME = "vector4";

            lua.NewMetaTable(META);

            lua.PushSafeCFunction("__add", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec4(L, ToVec4(L, 1) + ToVec4(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__sub", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec4(L, ToVec4(L, 1) - ToVec4(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__mul", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                if (L.IsNumber(1))
                    PushVec4(L, ToVec4(L, 2) * (float)L.ToNumber(1));
                else
                    PushVec4(L, ToVec4(L, 1) * (float)L.ToNumber(2));
                return 1;
            });

            lua.PushSafeCFunction("__unm", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                PushVec4(L, -ToVec4(L, 1));
                return 1;
            });

            lua.PushSafeCFunction("__eq", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                L.PushBoolean(ToVec4(L, 1) == ToVec4(L, 2));
                return 1;
            });

            lua.PushSafeCFunction("__tostring", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                var v = ToVec4(L, 1);
                L.PushString($"Vector4({v.X}, {v.Y}, {v.Z})");
                return 1;
            });

            lua.PushCopy(-1);
            lua.SetField(-2, "__index");

            lua.Pop(1);
        }

        public static void PushVec4(this Lua lua, Vector4 vec)
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

        public static Vector4 ToVec4(this Lua lua, int idx)
        {
            return new Vector4(
                (float)lua.GetNumField(idx, "x"), 
                (float)lua.GetNumField(idx, "y"), 
                (float)lua.GetNumField(idx, "z"),
                (float)lua.GetNumField(idx, "w")
            );
        }
    }
}