using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Physics
{
    public class TriangleMeshShape : BvhTriangleMeshShape
    {
        public TriangleIndexVertexArray Mesh { get; }

        public TriangleMeshShape(TriangleIndexVertexArray mesh, bool quantize) : base(mesh, quantize)
        {
            Mesh = mesh;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !Mesh.IsDisposed) Mesh.Dispose();
        }

        public static TriangleMeshShape CreateFromTriangleList(
            ICollection<Vector3> vertices,
            ICollection<int> indices,
            bool quantize
        ) {
            if (indices.Count == 0 || indices.Count % 3 != 0)
                throw new Exception($"Invalid triangle mesh: {indices.Count} indices");
            if (indices.Any(i => i < 0 || i >= vertices.Count))
                throw new Exception("Triangle mesh has out-of-range indices");

            return new TriangleMeshShape(new(indices, vertices), quantize);
        }
    }
}
