using KumaEngine.API;
using KumaEngine.Input;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.UI
{
    public class UISurface : IDisposable
    {
        const float MIN_KEY_INTERVALL = 0.03f;
        const float KEY_INTERVALL = 0.2f;

        Context ctx;
        ElementDocument doc = null!;

        float keyrepeat = 0;

        uint keytriggers = 0;

        Vector2 cppos = Vector2.Zero;

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
            

            doc = ctx.LoadDocument(AssetRetriver.FetchAssetPath(AssetKind.UI, rml))!;
            Visible = true;
        }

        public Element? GetElementById(string id) => doc.GetElementById(id);
        public void Resize(uint w,uint h) => ctx.SetDimensions((int)w, (int)h);
        public void Update(float dt)
        {
            bool triggerkey = false;

            if (keyrepeat >= Math.Max(MIN_KEY_INTERVALL, KEY_INTERVALL / ((float)keytriggers + 1)))
            {
                triggerkey = true;
                keyrepeat = 0;
            }

            var cpos = InputTracker.MousePosition;
            if (cppos != cpos)
            {
                ctx.ProcessMouseMove((int)cpos.X, (int)cpos.Y,RmlUiNet.Input.KeyModifier.None);

                cppos = cpos;
            }

            if (ctx.IsMouseInteracting)
            {
                if (InputTracker.ScrollDelta != 0)
                    ctx.ProcessMouseWheel(new(0, InputTracker.ScrollDelta), RmlUiNet.Input.KeyModifier.None);

                foreach (var item in InputTracker.GetMouseButtons())
                    ctx.ProcessMouseButtonDown((int)item, RmlUiNet.Input.KeyModifier.None);

                foreach (var item in InputTracker.GetReleasedMouseButtons())
                    ctx.ProcessMouseButtonUp((int)item, RmlUiNet.Input.KeyModifier.None);
            }

            if (ctx.GetFocusElement() != null)
            {
                string keydata = "";

                foreach (var item in InputTracker.GetPressedKeyChars())
                    keydata += item;

                bool triggered = false;

                foreach (var item in KeyProcessor)
                {
                    if (triggerkey && InputTracker.GetKey(item.Key))
                    {
                        triggered = true;
                        ctx.ProcessKeyDown(item.Value, RmlUiNet.Input.KeyModifier.None);
                    }

                    if (InputTracker.GetKeyRelesed(item.Key))
                        ctx.ProcessKeyUp(item.Value, RmlUiNet.Input.KeyModifier.None);
                }

                if (triggerkey)
                {
                    if (triggered) keytriggers++;
                    else keytriggers = 0;
                }

                ctx.ProcessTextInput(keydata);
            }

            keyrepeat += dt;

            ctx.Update();
        }
        public void Render() => ctx.Render();

        static Dictionary<Key, RmlUiNet.Input.KeyIdentifier> KeyProcessor = new()
        {
            { Key.Up, RmlUiNet.Input.KeyIdentifier.KI_UP },
            { Key.Down, RmlUiNet.Input.KeyIdentifier.KI_DOWN },
            { Key.Left, RmlUiNet.Input.KeyIdentifier.KI_LEFT },
            { Key.Right, RmlUiNet.Input.KeyIdentifier.KI_RIGHT },

            { Key.Enter, RmlUiNet.Input.KeyIdentifier.KI_RETURN },
            { Key.Escape, RmlUiNet.Input.KeyIdentifier.KI_ESCAPE },
            { Key.Space, RmlUiNet.Input.KeyIdentifier.KI_SPACE },
            { Key.Tab, RmlUiNet.Input.KeyIdentifier.KI_TAB },
            { Key.BackSpace, RmlUiNet.Input.KeyIdentifier.KI_BACK },
            { Key.Insert, RmlUiNet.Input.KeyIdentifier.KI_INSERT },
            { Key.Delete, RmlUiNet.Input.KeyIdentifier.KI_DELETE },
            { Key.PageUp, RmlUiNet.Input.KeyIdentifier.KI_PRIOR },
            { Key.PageDown, RmlUiNet.Input.KeyIdentifier.KI_NEXT },
            { Key.Home, RmlUiNet.Input.KeyIdentifier.KI_HOME },
            { Key.End, RmlUiNet.Input.KeyIdentifier.KI_END },
            { Key.CapsLock, RmlUiNet.Input.KeyIdentifier.KI_CAPITAL },
            { Key.ScrollLock, RmlUiNet.Input.KeyIdentifier.KI_SCROLL },
            { Key.PrintScreen, RmlUiNet.Input.KeyIdentifier.KI_SNAPSHOT },
            { Key.Pause, RmlUiNet.Input.KeyIdentifier.KI_PAUSE },
            { Key.NumLock, RmlUiNet.Input.KeyIdentifier.KI_NUMLOCK },
            { Key.Clear, RmlUiNet.Input.KeyIdentifier.KI_CLEAR },
            { Key.Sleep, RmlUiNet.Input.KeyIdentifier.KI_SLEEP },
        };
    }
}
