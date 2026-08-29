using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaCLI
{
    public static class PropertiesManager
    {
        public static GraphicsBackend GetBackend(string[] props)
        {
            if (props.Contains("-d3d11") || props.Contains("-dx11") || props.Contains("-d3d") || props.Contains("-dx"))
                return GraphicsBackend.Direct3D11;
            else if (props.Contains("-vk"))
                return GraphicsBackend.Vulkan;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GraphicsBackend.Direct3D11;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GraphicsBackend.Metal;
            else return GraphicsBackend.Vulkan;
        }
    }
}
