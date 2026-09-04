using Assimp;
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
    public static class ModelAPI
    {
        public static Dictionary<int,Model> ModelHandles = new();

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("fromFile",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var fileName = lua.ToString(1);

                var mdls = Model.FromFile(fileName);

                foreach (var item in mdls)
                    lua.PushModel(item);

                return mdls.Count;
            }),
            DefinitionFile.RegisterFunction("new",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                lua.PushModel(new());
                return 1;
            })
        ];

        public static void PushModel(this Lua lua,Model mdl)
        {
            var handle = 0;

            if (ModelHandles.ContainsValue(mdl))
                handle = ModelHandles.First(x => x.Value == mdl).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                ModelHandles.Add(handle, mdl);
            }

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction(_ =>
            {
                ModelHandles[handle].Invalidate();

                return 0;
            });
            lua.SetField(-2, "invalidate");

            lua.PushNumList(ModelHandles[handle].Indices);
            lua.SetField(-2, "indices");

            {
                lua.NewTable();

                lua.PushVector3List(ModelHandles[handle].Vertices.Vertices);
                lua.SetField(-2, "positions");

                lua.PushVector3List(ModelHandles[handle].Vertices.Normals);
                lua.SetField(-2, "normals");

                lua.PushVector3List(ModelHandles[handle].Vertices.Tangents);
                lua.SetField(-2, "tangents");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    L.PushVector3List(ModelHandles[handle].Vertices.UVWLayers[key]);
                    return 1;
                });
                lua.SetField(-2, "getUVWLayer");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    L.PushVector4List(ModelHandles[handle].Vertices.ColorLayers[key]);
                    return 1;
                });
                lua.SetField(-2, "getColorLayer");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    ModelHandles[handle].Vertices.UVWLayers.Add(new(ModelHandles[handle].Vertices.Vertices.Count));
                    return 1;
                });
                lua.SetField(-2, "addUVWLayer");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    ModelHandles[handle].Vertices.ColorLayers.Add(new(ModelHandles[handle].Vertices.Vertices.Count));
                    return 1;
                });
                lua.SetField(-2, "addColorLayer");

                lua.PushInteger(ModelHandles[handle].Vertices.UVWLayers.Count);
                lua.SetField(-2, "uvwLayerCount");

                lua.PushInteger(ModelHandles[handle].Vertices.ColorLayers.Count);
                lua.SetField(-2, "colorLayerCount");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    int vertex = (int)L.ToInteger(2);
                    Vector3 uvw = L.ToVec3(3);

                    var layer = ModelHandles[handle].Vertices.UVWLayers[key];

                    if (vertex == layer.Count) layer.Add(uvw);
                    else layer[vertex] = uvw;

                    return 1;
                });
                lua.SetField(-2, "setUVW");

                lua.PushCFunction(ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    int vertex = (int)L.ToInteger(2);
                    Vector4 color = L.ToVec4(3);

                    var layer = ModelHandles[handle].Vertices.ColorLayers[key];

                    if (vertex == layer.Count) layer.Add(color);
                    else layer[vertex] = color;

                    return 1;
                });
                lua.SetField(-2, "setColor");
            }
            lua.SetField(-2, "vertices");

            lua.PushInteger(ModelHandles[handle].Vertices.Vertices.Count);
            lua.SetField(-2, "vertexCount");

            lua.PushInteger(ModelHandles[handle].Indices.Count);
            lua.SetField(-2, "indexCount");

            lua.PushCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 pos = L.ToVec3(1);
                Vector3 normal = L.ToVec3(2);
                Vector3 tangent = L.ToVec3(3);

                ModelHandles[handle].Vertices.Vertices.Add(pos);
                ModelHandles[handle].Vertices.Normals.Add(normal);
                ModelHandles[handle].Vertices.Tangents.Add(tangent);
                return 1;
            });
            lua.SetField(-2, "addVertex");

            lua.PushCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                uint index = (uint)L.ToInteger(1);

                ModelHandles[handle].Indices.Add(index);

                return 1;
            });
            lua.SetField(-2, "addIndex");
        }

        public static Model ToModel(this Lua lua,int index)
        {
            int val = (int)lua.GetIntField(index,"handle");
            if (ModelHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid model handle");
        }

        public static void PushNumList<T>(this Lua lua, List<T> list) where T : INumber<T> =>
            PushList(lua,list,
                (l,v) => l.PushNumber(long.CreateTruncating(v)), 
                (l,_) => T.CreateTruncating(l.ToNumber(3))
            );

        public static void PushVector3List(this Lua lua, List<Vector3> list) =>
            PushList(lua,list,(l,v) => l.PushVec3(v), (l, _) => l.ToVec3(3));
        public static void PushVector4List(this Lua lua, List<Vector4> list) =>
            PushList(lua, list, (l, v) => l.PushVec4(v), (l,_) => l.ToVec4(3));

        public static void PushList<T>(this Lua lua, List<T> list, Action<Lua,T> push, Func<Lua, int, T> get)
        {
            lua.NewTable();
            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                int key = (int)L.ToInteger(2) - 1;

                push(lua,list[key]);
                return 1;
            });
            lua.SetField(-2, "__index");

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                int key = (int)L.ToInteger(2) - 1;

                list[key] = get(lua,key);

                return 0;
            });
            lua.SetField(-2, "__newindex");

            lua.SetMetaTable(-2);
        }
    }
}
