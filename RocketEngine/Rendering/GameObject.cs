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
        public KumaPipeline Pipeline = null!;
        public Model Model = null!;
        public KumaMaterial Material = null!;

        public GameObject Parent = null!;

        Matrix4x4 _PTransform = Matrix4x4.Identity;
        Matrix4x4 _LTransform = Matrix4x4.Identity;
        Matrix4x4 _ATransform = Matrix4x4.Identity;

        public string Name;
        public Matrix4x4 Transform { get => GetTransform(); set => SetTransform(value); }

        void SetTransform(Matrix4x4 value)
        {
            _LTransform = value;
            UpdateTransformParent();
        }

        Matrix4x4 GetTransform()
        {
            UpdateTransformParent();
            return _ATransform;
        }

        void UpdateTransformParent()
        {
            if (Parent is null)
            {
                _ATransform = _LTransform;
                return;
            }

            if (Parent.Transform != _PTransform) _PTransform = Parent.Transform;

            GameObjectAPI.DecomposeTransform(_LTransform, out var pos, out var angle, out var scale);
            GameObjectAPI.DecomposeTransform(_PTransform, out var ppos, out var pangle, out var pscale);

            _ATransform = GameObjectAPI.ComposeTransform(ppos + pos, pangle + angle, pscale * scale);
        }

        public GameObject(string name,KumaPipeline pipeline,Model model)
        {
            this.Name = name;
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
