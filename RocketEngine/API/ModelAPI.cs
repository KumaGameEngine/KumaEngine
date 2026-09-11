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

                Model mdl = new();

                lua.PushModel(mdl);
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

            var MODNAME = "model." + handle;

            var model = ModelHandles[handle];

            lua.NewTable();

            lua.PushInteger(handle);
            lua.SetField(-2, "handle");

            lua.PushSafeCFunction("invalidate", MODNAME, _ =>
            {
                ModelHandles[handle].Invalidate();

                return 0;
            });

            lua.PushNumList(model.Indices);
            lua.SetField(-2, "indices");

            {
                lua.NewTable();

                lua.PushVector3List(model.Vertices.Vertices);
                lua.SetField(-2, "positions");

                lua.PushVector3List(model.Vertices.Normals);
                lua.SetField(-2, "normals");

                lua.PushVector3List(model.Vertices.Tangents);
                lua.SetField(-2, "tangents");

                lua.PushSafeCFunction("getUVWLayer", MODNAME, ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    L.PushVector3List(model.Vertices.UVWLayers[key]);
                    return 1;
                });

                lua.PushSafeCFunction("getColorLayer", MODNAME, ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    int key = (int)L.ToInteger(1);
                    L.PushVector4List(ModelHandles[handle].Vertices.ColorLayers[key]);
                    return 1;
                });

                lua.PushSafeCFunction("addUVWLayer", MODNAME, ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    ModelHandles[handle].Vertices.UVWLayers.Add(new(ModelHandles[handle].Vertices.Vertices.Count));
                    return 1;
                });

                lua.PushSafeCFunction("addColorLayer", MODNAME, ilua =>
                {
                    var L = Lua.FromIntPtr(ilua);
                    ModelHandles[handle].Vertices.ColorLayers.Add(new(ModelHandles[handle].Vertices.Vertices.Count));
                    return 1;
                });

                lua.PushInteger(model.Vertices.UVWLayers.Count);
                lua.SetField(-2, "uvwLayerCount");

                lua.PushInteger(model.Vertices.ColorLayers.Count);
                lua.SetField(-2, "colorLayerCount");

                lua.PushSafeCFunction("setUVW", MODNAME, ilua =>
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

                lua.PushSafeCFunction("setColor", MODNAME, ilua =>
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
            }
            lua.SetField(-2, "vertices");

            lua.PushInteger(model.Vertices.Vertices.Count);
            lua.SetField(-2, "vertexCount");

            lua.PushInteger(model.Indices.Count);
            lua.SetField(-2, "indexCount");

            lua.PushSafeCFunction("addVertex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                Vector3 pos = L.ToVec3(1);
                Vector3 normal = L.ToVec3(2);
                Vector3 tangent = L.ToVec3(3);

                var mdl = ModelHandles[handle];

                mdl.Vertices.Vertices.Add(pos);
                mdl.Vertices.Normals.Add(normal);
                mdl.Vertices.Tangents.Add(tangent);
                return 1;
            });

            lua.PushSafeCFunction("addIndex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                uint index = (uint)L.ToInteger(1);

                ModelHandles[handle].Indices.Add(index);

                return 1;
            });
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
            const string MODNAME = "list";

            lua.NewTable();
            lua.NewTable();

            lua.PushSafeCFunction("__index", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                int key = (int)L.ToInteger(2) - 1;

                push(lua,list[key]);
                return 1;
            });

            lua.PushSafeCFunction("__newindex", MODNAME, ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                int key = (int)L.ToInteger(2) - 1;

                list[key] = get(lua,key);

                return 0;
            });

            lua.SetMetaTable(-2);
        }
    }
}
