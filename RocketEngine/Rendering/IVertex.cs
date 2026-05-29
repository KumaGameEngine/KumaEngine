using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KumaEngine.Rendering
{
    public interface IVertex
    {
        public static abstract VertexLayoutDescription Layout { get; set; }

        public static abstract uint Size { get; set; }
    }
}
