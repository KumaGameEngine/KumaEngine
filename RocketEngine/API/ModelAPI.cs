using KeraLua;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var pipeline = lua.ToPipeline(2);

                lua.PushModel(
                    Model.FromFile(
                        DefinitionFile.Game.GraphicsDevice,
                        DefinitionFile.Game.ResourceFactory,
                        pipeline, fileName
                    )[0]
                );

                return 1;
            })
        ];

        public static void PushModel(this Lua lua,Model mdl)
        {
            var handle = Random.Shared.Next(int.MinValue, int.MaxValue);

            ModelHandles.Add(handle, mdl);

            lua.PushInteger(handle);
        }

        public static Model ToModel(this Lua lua,int index)
        {
            int val = (int)lua.ToInteger(index);
            if (ModelHandles.TryGetValue(val, out var mdl)) return mdl;
            throw new Exception("Invalid model handle");
        }
    }
}
