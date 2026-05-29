using KeraLua;
using RmlUiNet;
using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace KumaEngine.API
{
    public static class UIAPI
    {
        public static string CurrentDocument = "";
        static ElementDocument doc = null!;

        public static void ShowCurrentDocument()
        {
            try
            {
                if (DefinitionFile.Game.rmlContext != null)
                {
                    doc = DefinitionFile.Game.rmlContext!.LoadDocument(CurrentDocument)!;
                    if (doc != null) doc.Show();
                }
            }
            catch {}
        }

        public static LuaRegister[] Register = 
        [
            DefinitionFile.RegisterFunction("loadRml",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var d = lua.ToString(1);

                CurrentDocument = Path.Combine("Data","UI",d);

                ShowCurrentDocument();

                return 1;
            }),
            DefinitionFile.RegisterFunction("getElementById",ilua =>
            {
                var lua = Lua.FromIntPtr(ilua);
                var id = lua.ToString(1);

                lua.PushElement(id);

                return 1;
            })
        ];

        public static void PushElement(this Lua lua, string id)
        {
            lua.NewTable();

            lua.PushSafeCFunction(ilua =>
            {
                var L = Lua.FromIntPtr(ilua);
                string p = L.ToString(1);
                string v = L.ToString(2);

                doc.GetElementById(id)!.SetAttribute(p, v);

                return 0;
            });
            lua.SetField(-2, "setAttribute");
        }

    }
}
