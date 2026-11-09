using KumaEngine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class KumaNativeSwapchain : KumaSwapchain
    {
        public KumaNativeSwapchain() : base()
        {
            Framebuffer = null!;
            ClearDepth = false;
        }
    }
}
