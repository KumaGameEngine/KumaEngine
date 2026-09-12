using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.UI
{
    public class KumaSystemInterface : SystemInterface
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        public override double ElapsedTime => _stopwatch.Elapsed.TotalMilliseconds;

        public override void ActivateKeyboard(float caretX, float caretY, float lineHeight)
        {
            
        }

        public override void DeactivateKeyboard()
        {
            
        }

        public override string GetClipboardText()
        {
            return "";
        }

        public override void SetClipboardText(string text)
        {
            
        }

        public override string JoinPath(string path, string file)
        {
            return file;
        }
    }
}
