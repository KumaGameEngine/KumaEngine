/*
    Taken from: https://github.com/mellinoe/veldrid-samples/tree/master/src/SampleBase
    Modified by: RocketEngine Team
*/

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class Camera
    {
        private float _fov = 1f;
        private float _refdist = 3f;
        private float _near = .1f;
        private float _far = 1000f;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        public static DeviceBuffer _cameraProjViewBuffer;
        public static DeviceBuffer _cameraProjViewInverseBuffer;

        public static DeviceBuffer _cameraPosBuffer;

        private Vector3 _position = new Vector3(0, 3, 0);
        private Vector3 _rotation = new Vector3(0, 0, 0);
        private Vector3 _lookDirection = new Vector3(0, -.3f, -1f);
        private float _moveSpeed = 10.0f;

        private float _windowWidth;
        private float _windowHeight;

        public event Action<Matrix4x4> ProjectionChanged;
        public event Action<Matrix4x4> ViewChanged;

        bool _ortho;
        public bool Orthographic
        {
            get => _ortho; set
            {
                _ortho = value;
                UpdatePerspectiveMatrix();
            }
        }

        public Camera(float width, float height,ResourceFactory factory)
        {
            _windowWidth = width;
            _windowHeight = height;

            UpdatePerspectiveMatrix();
            UpdateViewMatrix();
        }

        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjectionMatrix => _projectionMatrix;

        public Vector3 Position { get => _position; set { _position = value; UpdateViewMatrix();  } }
        public Vector3 Rotation { get => _rotation; set { _rotation = value; UpdateViewMatrix(); } }

        Vector3 _rotationRadians => Rotation * (MathF.PI / 180f);

        public float FarDistance { get => _far; set { _far = value; UpdatePerspectiveMatrix(); } }
        public float FieldOfView { get => _fov; set { _fov = value; } }
        public float OrthographicSize { get => _refdist; set { _refdist = value; } }
        public float NearDistance { get => _near; set { _near = value; UpdatePerspectiveMatrix(); } }

        public float AspectRatio => _windowWidth / _windowHeight;

        public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }
        public Vector3 Forward => GetLookDir();
        public Vector3 right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

        public void Update(CommandList cl)
        {
            cl.UpdateBuffer(_cameraProjViewBuffer,0, new MatrixPair(_viewMatrix, _projectionMatrix));
            cl.UpdateBuffer(_cameraProjViewInverseBuffer,0, new MatrixPair(Matrix4x4.Invert(_viewMatrix, out var viewInverse) ? viewInverse : _viewMatrix, Matrix4x4.Invert(_projectionMatrix, out var projInverse) ? projInverse : _projectionMatrix));
            cl.UpdateBuffer(_cameraPosBuffer, 0, GetCameraInfo());
        }

        private float Clamp(float value, float min, float max)
        {
            return value > max
                ? max
                : value < min
                    ? min
                    : value;
        }

        public void WindowResized(float width, float height)
        {
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
        }

        private void UpdatePerspectiveMatrix()
        {
            if (Orthographic)
            {
                UpdateOrthoMatrix();
                return;
            }

            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(_fov, _windowWidth / _windowHeight, _near, _far);
            ProjectionChanged?.Invoke(_projectionMatrix);
        }
        private void UpdateOrthoMatrix()
        {
            float coeff = _windowWidth / _windowHeight;
            float halfh = MathF.Tan(_fov / 2f) * _refdist;
            float halfw = halfh * coeff;

            _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-halfw, halfw, -halfh, halfh, _near, _far);
            ProjectionChanged?.Invoke(_projectionMatrix);
        }

        private void UpdateViewMatrix()
        {
            Vector3 lookDir = GetLookDir();

            Quaternion lookRotation = Quaternion.CreateFromYawPitchRoll(
                _rotationRadians.Y, 
                _rotationRadians.X, 
                _rotationRadians.Z
            );

            Vector3 up = Vector3.Transform(Vector3.UnitY, lookRotation);

            _lookDirection = lookDir;
            _viewMatrix = Matrix4x4.CreateLookAt(_position, _position + _lookDirection, up);
            ViewChanged?.Invoke(_viewMatrix);
        }

        private Vector3 GetLookDir()
        {
            Quaternion lookRotation = Quaternion.CreateFromYawPitchRoll(
                _rotationRadians.Y, 
                _rotationRadians.X, 
                _rotationRadians.Z
            );

            Vector3 lookDir = Vector3.Transform(-Vector3.UnitZ, lookRotation);
            return lookDir;
        }

        public void LookAt(Vector3 target)
        {
            Vector3 dir = Vector3.Normalize(target - _position);

            var yaw = MathF.Atan2(-dir.X, -dir.Z);
            var pitch = MathF.Asin(dir.Y);

            Rotation = new Vector3(pitch, yaw, 0) * (180 / MathF.PI);
        }

        public CameraInfo GetCameraInfo() => new CameraInfo
        {
            Position = _position,
            ScreenSize = new(_windowWidth,_windowHeight)
        };

        public static Vector3 EulerToDirection(Vector3 vec)
        {
            float pitch = vec.X * MathF.PI / 180f;
            float yaw = vec.Y * MathF.PI / 180f;
            float roll = vec.Z * MathF.PI / 180f;

            Quaternion q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
            Vector3 direction = Vector3.Transform(-Vector3.UnitZ, q);

            return Vector3.Normalize(direction);
        }
    }

    [StructLayout(LayoutKind.Sequential,Pack = 1)]
    public struct CameraInfo
    {
        public Vector3 Position;
        float _pad0;
        public Vector2 ScreenSize;
        Vector2 _pad1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MatrixPair(Matrix4x4 first, Matrix4x4 second)
    {
        public Matrix4x4 First = first;
        public Matrix4x4 Second = second;
        public Matrix4x4 Third = first * second;
    }
}
