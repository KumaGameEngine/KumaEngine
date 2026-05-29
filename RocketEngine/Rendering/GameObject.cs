using KumaEngine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Rendering
{
    public class GameObject
    {
        public KumaPipeline Pipeline;
        public Model Model;
        public KumaMaterial Material = null!;

        public Matrix4x4 Transform = Matrix4x4.Identity;

        public GameObject(KumaPipeline pipeline,Model model)
        {
            this.Pipeline = pipeline;
            this.Model = model;
        }

        public void Destroy()
        {
            var thiskey = GameObjectAPI.GameObjectHandles.First(x => x.Value == this);
            GameObjectAPI.GameObjectHandles.Remove(thiskey.Key);
        }
    }
}
