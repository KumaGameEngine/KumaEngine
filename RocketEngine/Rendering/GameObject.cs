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
        const float EULER_TO_RAD = MathF.PI / 180f;

        public KumaPipeline Pipeline = null!;
        public Model Model = null!;
        public KumaMaterial Material = null!;

        public GameObject Parent = null!;

        Matrix4x4 _PTransform = Matrix4x4.Identity;
        Matrix4x4 _LTransform = Matrix4x4.Identity;
        Matrix4x4 _ATransform = Matrix4x4.Identity;

        Vector3 _Pos, _Rot, _Size = Vector3.One;

        public string Name;

        public Vector3 Position { get => _Pos; set { _Pos = value; UpdateTransform(); } }
        public Vector3 Rotation { get => _Rot; set { _Rot = value; UpdateTransform(); } }
        public Vector3 Size { get => _Size; set { _Size = value; UpdateTransform(); } }

        public Matrix4x4 Transform { get => GetTransform(); private set => SetTransform(value); }

        void UpdateTransform()
        {
            Transform = Matrix4x4.CreateScale(_Size)
                 * Matrix4x4.CreateFromYawPitchRoll(
                     _Rot.Y * EULER_TO_RAD, 
                     _Rot.X * EULER_TO_RAD, 
                     _Rot.Z * EULER_TO_RAD
                 ) * Matrix4x4.CreateTranslation(_Pos);
        }

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
