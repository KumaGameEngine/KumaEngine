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

        Matrix4x4 _GTransform = Matrix4x4.Identity;
        Matrix4x4 _PTransformCache = Matrix4x4.Identity;

        Vector3 _Pos, _Rot, _Size = Vector3.One;

        public string Name;

        public Vector3 Position { get => _Pos; set { _Pos = value; UpdateTransform(); } }
        public Vector3 Rotation { get => _Rot; set { _Rot = value; UpdateTransform(); } }
        public Vector3 Size { get => _Size; set { _Size = value; UpdateTransform(); } }

        public Matrix4x4 Transform { get => GetTransform(); private set => SetTransform(value); }

        void UpdateTransform()
        {
            var pos = _Pos;
            var rot = _Rot;
            var size = _Size;

            if (Parent is not null)
            {
                pos += Parent.Position;
                rot += Parent.Rotation;
                size *= Parent.Size;

                _PTransformCache = Parent.Transform;
            }

            Transform = Matrix4x4.CreateScale(size)
                 * Matrix4x4.CreateFromYawPitchRoll(
                     rot.Y * EULER_TO_RAD, 
                     rot.X * EULER_TO_RAD, 
                     rot.Z * EULER_TO_RAD
                 ) * Matrix4x4.CreateTranslation(pos);
        }

        void SetTransform(Matrix4x4 value) => _GTransform = value;

        Matrix4x4 GetTransform()
        {
            if (Parent is not null && _PTransformCache != Parent.Transform)
                UpdateTransform();

            return _GTransform;
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
