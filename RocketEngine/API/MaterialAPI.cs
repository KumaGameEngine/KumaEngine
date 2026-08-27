using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.API
{
    public static class MaterialAPI
    {
        public static Dictionary<int, KumaMaterial> PipelineHandles = new();

        public static LuaRegister[] Register =
        [
            DefinitionFile.RegisterFunction("fromFile",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var setName = lua.ToString(1);

                lua.PushMaterial(
                    KumaMaterial.FromFile(DefinitionFile.Game.GraphicsDevice,DefinitionFile.Game.ResourceFactory,setName)
                );

                return 1;
            })
        ];

        public static void PushMaterial(this Lua lua,KumaMaterial mat)
        {
            var handle = 0;

            if (PipelineHandles.ContainsValue(mat))
                handle = PipelineHandles.First(x => x.Value == mat).Key;
            else
            {
                handle = Random.Shared.Next(int.MinValue, int.MaxValue);
                PipelineHandles.Add(handle, mat);
            }

            lua.PushInteger(handle);
        }

        public static KumaMaterial ToMaterial(this Lua lua, int index)
        {
            int val = (int)lua.ToInteger(index);
            if (PipelineHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid model handle");
        }
    }
}
