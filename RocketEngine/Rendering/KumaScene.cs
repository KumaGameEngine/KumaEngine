using BulletSharp;
using System.Numerics;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class KumaScene
    {
        public static KumaScene CurrentScene = null!;
        public static DeviceBuffer PointLightBuffer = null!;

        public List<GameObject> GameObjects = new();
        public KumaMaterial SkyboxMat = null!;
        public List<KumaLight> Lights = new();
        public KumaSun Sun = KumaSun.Default;
        public Camera Camera;

        public DynamicsWorld World;
        public Vector3 Gravity = new(0,-9.81f,0);

        public KumaScene(Camera cam)
        {
            Camera = cam;

            var collisionConf = new DefaultCollisionConfiguration();
            var dispatcher = new CollisionDispatcher(collisionConf);
            var broadphase = new DbvtBroadphase();
            var solver = new SequentialImpulseConstraintSolver();

            World = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConf);
            World.SetGravity(ref Gravity);
        }

        public static void CreateLightBuffers(ResourceFactory factory)
        {
            if (PointLightBuffer == null)
                PointLightBuffer = factory.CreateBuffer(
                    new((uint)LightUploadScheme.Size, BufferUsage.Dynamic | BufferUsage.UniformBuffer)
                );
        }

        public static unsafe void UploadLights(GraphicsDevice device)
        {
            var sorted = CurrentScene.Lights
                .OrderByDescending(light =>
                {
                    float dist = Vector3.Distance(CurrentScene.Camera.Position, light.Position);
                    return light.Intensity / dist;
                })
                .Take(LightUploadScheme.MAX_POINT_LIGHTS)
                .ToList();

            LightUploadScheme scheme = new();
            scheme.PointLightCount = (uint)sorted.Count;
            for (int i = 0; i < sorted.Count; i++)
                scheme.Lights[i] = sorted[i].reference;

            scheme.Sun = CurrentScene.Sun.reference;

            device.UpdateBuffer(PointLightBuffer, 0, ref scheme);
        }

        public static void SwitchScene(KumaScene scene,GraphicsDevice device,ResourceFactory factory)
        {
            CurrentScene = scene;

            CreateLightBuffers(factory);
        }
    }
}
