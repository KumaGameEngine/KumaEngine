using KumaEngine.API;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.UI
{
    public class UISurface : IDisposable
    {
        Context ctx;
        ElementDocument doc = null!;

        bool _visible = true;
        public bool Visible
        {
            get => _visible; set
            {
                _visible = value;
                if (value) doc.Show();
                else doc.Hide();
            }
        }

        public UISurface(string name, string rml)
        {
            ctx = Rml.CreateContext(
                name,
                new(
                    (int)DefinitionFile.Game.Window.Width,
                    (int)DefinitionFile.Game.Window.Height
                )
            )!;

            LoadDocument(rml);
        }

        public void Dispose() => Rml.RemoveContext(ctx.Name);

        public void LoadDocument(string rml)
        {
            doc = ctx.LoadDocument(Path.Combine("Data", "UI", rml))!;
            Visible = true;
        }

        public Element? GetElementById(string id) => doc.GetElementById(id);
        public void Resize(uint w,uint h) => ctx.SetDimensions((int)w, (int)h);
        public void Update() => ctx.Update();
        public void Render() => ctx.Render();
    }
}
