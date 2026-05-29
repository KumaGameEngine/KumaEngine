using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.API
{
    public static class PipelineAPI
    {
        public static Dictionary<int, KumaPipeline> PipelineHandles = new();

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("fromShaderSet",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var setName = lua.ToString(1);

                lua.PushPipeline(
                    KumaPipeline.FromSet(DefinitionFile.Game.ResourceFactory,setName)
                );

                return 1;
            })
        ];

        public static void PushPipeline(this Lua lua,KumaPipeline mdl)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);

            PipelineHandles.Add(handle, mdl);

            lua.PushInteger(handle);
        }

        public static KumaPipeline ToPipeline(this Lua lua, int index)
        {
            int val = (int)lua.ToInteger(index);
            if (PipelineHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid model handle");
        }
    }
}
