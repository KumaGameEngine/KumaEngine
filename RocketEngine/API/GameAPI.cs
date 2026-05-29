using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.API
{
    public static class GameAPI
    {
        public static Action<float> GameUpdate = (dt) => { };

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("bindUpdate",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var func = lua.GetFunction(1);

                GameUpdate += (dt) =>
                {
                    int stackBefore = DefinitionFile.lua.GetTop();

                    DefinitionFile.lua.RawGetInteger(LuaRegistry.Index, func);
                    DefinitionFile.lua.PushNumber(dt);
                    var status = DefinitionFile.lua.PCall(1, 0, 0);

                    if (status != LuaStatus.OK)
                    {
                        string err = DefinitionFile.lua.ToString(-1);
                        DefinitionFile.lua.Pop(1);
                        Console.WriteLine($"Lua error: {err}");
                    }

                    int stackAfter = DefinitionFile.lua.GetTop();
                    if (stackAfter != stackBefore)
                        Console.WriteLine($"Stack leak: was {stackBefore}, now {stackAfter}");

                    DefinitionFile.lua.GarbageCollector(LuaGC.Step, 10000);
                };

                return 1;
            })
        ];
    }
}
