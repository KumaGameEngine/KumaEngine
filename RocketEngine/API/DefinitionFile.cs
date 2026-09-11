using Assimp;
using KeraLua;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.API
{
    public static class DefinitionFile
    {
        public static Lua lua;
        public static Game Game;

        static Dictionary<string, LuaRegister[]> EngineLib = new()
        {
            {"model",ModelAPI.Register},
            {"pipeline",PipelineAPI.Register},
            {"scene",SceneAPI.Register},
            {"gameObject",GameObjectAPI.Register},
            {"physics",PhysicsAPI.Register},
            {"input",InputAPI.Register},
            {"game",GameAPI.Register},
            {"vector3",Vector3API.Register},
            {"vector4",Vector4API.Register},
            {"light",LightAPI.Register},
            {"material",MaterialAPI.Register},
            {"ui",UIAPI.Register},
        };

        public static void Init()
        {
            lua = new();

            lua.DoString($"package.path = '{Path.Combine("Data","Scripts", "?.lua").Replace("\\", "/")}';");

            Vector3API.RegisterMeta(lua);

            lua.GetGlobal("package");
            lua.GetField(-1, "preload");
            lua.PushSafeCFunction(ilua =>
            {
                lua.NewTable();

                foreach (var item in EngineLib) PushSection(item.Key, item.Value);

                PushEnum<Key>("key");
                PushEnum<MouseButton>("mouseButton");

                return 1;
            });
            lua.SetField(-2, "engine");
            lua.Pop(2);

            if (lua.DoFile(AssetRetriver.FetchAssetPath(AssetKind.Scripts,"main.lua")))
            {
                string errorMsg = lua.ToString(-1);
                Console.WriteLine($"Detected a Lua exception: {errorMsg}");

                lua.Pop(1);
            }
        }

        public static LuaRegister RegisterFunction(string name, LuaFunction function) => new LuaRegister()
        {
            name = name,
            function = function
        };
        static void PushEnum<T>(string name) where T : Enum
        {
            lua.NewTable();

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                lua.PushInteger(Convert.ToInt64(value));
                lua.SetField(-2, value.ToString());
            }

            MakeReadOnly();

            lua.SetField(-2, name);
        }
        public static void PushSection(string name, LuaRegister[] section)
        {
            var s = section.ToList();
            s.Add(new LuaRegister() { name = null, function = null });

            lua.NewLib(s.ToArray());
            MakeReadOnly();
            lua.SetField(-2, name);
        }

        static void MakeReadOnly()
        {
            lua.NewTable();
            lua.PushString("__newindex");
            lua.PushSafeCFunction(L =>
            {
                throw new Exception("Enum is read-only");
            });
            lua.SetTable(-3);
            lua.SetMetaTable(-2);
        }
    }

    public static class LuaExtensions
    {
        public static readonly List<LuaFunction> PinnedDelegates = new();

        public static void PushSafeCFunction(this Lua lua, LuaFunction fn)
        {
            PinnedDelegates.Add(fn);
            lua.PushCFunction(fn);
        }

        public static string GetStringField(this Lua lua, int stack, string name)
        {
            lua.GetField(stack, name);
            var result = lua.ToString(-1);
            lua.Pop(1);
            return result;
        }
        public static bool GetBooleanField(this Lua lua, int stack, string name)
        {
            lua.GetField(stack, name);
            var result = lua.ToBoolean(-1);
            lua.Pop(1);
            return result;
        }
        public static double GetNumField(this Lua lua, int stack, string name)
        {
            lua.GetField(stack, name);
            var result = lua.ToNumber(-1);
            lua.Pop(1);
            return result;
        }
        public static int GetIntField(this Lua lua, int stack, string name) => (int)GetNumField(lua, stack, name);
        public static int GetFunctionField(this Lua lua, int stack, string name)
        {
            lua.GetField(stack, name);

            if (!lua.IsFunction(-1))
            {
                lua.Pop(1);
                throw new InvalidOperationException($"{name} is not a function");
            }

            int funcRef = lua.Ref(LuaRegistry.Index);
            return funcRef;
        }
        public static int GetFunction(this Lua lua, int stack)
        {
            if (!lua.IsFunction(stack))
            {
                lua.Pop(1);
                return int.MinValue;
            }

            lua.PushCopy(stack);
            int funcRef = lua.Ref(LuaRegistry.Index);
            return funcRef;
        }

        public static LuaStatus CallLuaFunction(this Lua lua, int funcRef, int args = 0, int results = 0)
        {
            lua.RawGetInteger(LuaRegistry.Index, funcRef);
            return lua.PCall(args, results, 0);
        }
        public static bool LuaFieldExists(this Lua lua, int stack, string fieldName)
        {
            lua.GetField(stack, fieldName);
            bool exists = lua.Type(-1) != LuaType.Nil;

            lua.Pop(1);
            return exists;
        }
        public static int GetLuaFunctionArgRef(this Lua lua, int index)
        {
            if (!lua.IsFunction(index))
                throw new InvalidOperationException($"Argument at index {index} is not a function");
            lua.CheckStack(1);
            lua.Copy(index, lua.GetTop() + 1);
            return lua.Ref(LuaRegistry.Index);
        }

        public static string SerializeLuaValue(this Lua lua, int index)
        {
            var type = lua.Type(index);
            return type switch
            {
                LuaType.String => new JObject { ["t"] = "s", ["v"] = lua.ToString(index) }.ToString(Formatting.None),
                LuaType.Number => lua.IsInteger(index)
                                    ? new JObject { ["t"] = "i", ["v"] = lua.ToInteger(index) }.ToString(Formatting.None)
                                    : new JObject { ["t"] = "n", ["v"] = lua.ToNumber(index) }.ToString(Formatting.None),
                LuaType.Boolean => new JObject { ["t"] = "b", ["v"] = lua.ToBoolean(index) }.ToString(Formatting.None),
                LuaType.Nil => new JObject { ["t"] = "nil" }.ToString(Formatting.None),
                LuaType.Table => new JObject { ["t"] = "tbl", ["v"] = SerializeTable(lua, index) }.ToString(Formatting.None),
                _ => new JObject { ["t"] = "nil" }.ToString(Formatting.None)
            };
        }

        public static string SerializeTable(this Lua lua, int index)
        {
            if (index < 0) index = lua.GetTop() + index + 1;

            var dict = new JObject();
            lua.PushNil();

            while (lua.Next(index))
            {
                string key = lua.Type(-2) switch
                {
                    LuaType.String => lua.ToString(-2),
                    LuaType.Number => lua.ToNumber(-2).ToString(),
                    _ => null
                };

                if (key != null)
                    dict[key] = SerializeLuaValue(lua, -1);

                lua.Pop(1);
            }

            return dict.ToString(Formatting.None);
        }

        public static void DeserializeLuaValue(this Lua lua, string json)
        {
            PushJToken(lua, JObject.Parse(json));
        }

        public static void PushJToken(this Lua lua, JObject root)
        {
            var type = root["t"].Value<string>();
            switch (type)
            {
                case "s":
                    lua.PushString(root["v"].Value<string>());
                    break;
                case "i":
                    lua.PushInteger(root["v"].Value<long>());
                    break;
                case "n":
                    lua.PushNumber(root["v"].Value<double>());
                    break;
                case "b":
                    lua.PushBoolean(root["v"].Value<bool>());
                    break;
                case "tbl":
                    lua.NewTable();
                    var entries = JObject.Parse(root["v"].Value<string>());
                    foreach (var (k, v) in entries)
                    {
                        if (long.TryParse(k, out var intKey))
                            lua.PushInteger(intKey);
                        else
                            lua.PushString(k);

                        PushJToken(lua, JObject.Parse(v.Value<string>()));
                        lua.SetTable(-3);
                    }
                    break;
                default:
                    lua.PushNil();
                    break;
            }
        }
    }
}
