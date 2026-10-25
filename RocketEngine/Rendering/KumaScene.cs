using Assimp.Unmanaged;
using BulletSharp;
using KumaEngine.API;
using KumaEngine.Physics;
using System.Numerics;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class KumaScene : IDisposable
    {
        public static KumaScene CurrentScene = null!;

        public static ResourceLayout LightLayout = null!;
        Dictionary<Camera, DeviceBuffer> LightBuffers = new();
        Dictionary<Camera, ResourceSet> LightSets = new();

        public List<TriggerCollision> ActiveTriggers = new();
        public List<GameObject> GameObjects = new();
        public KumaMaterial SkyboxMat = null!;
        public List<KumaLight> Lights = new();
        public KumaSun Sun = KumaSun.Default;
        public List<Camera> Cameras { get; } = new();

        public bool LightsDirty = true;

        public DynamicsWorld World;
        public Vector3 Gravity = new(0,-9.81f,0);

        static KumaScene()
        {
            LightLayout = DefinitionFile.Game.ResourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "Lights", ResourceKind.UniformBuffer,
                        ShaderStages.Fragment | ShaderStages.Vertex)));
        }

        public KumaScene(Camera cam)
        {
            AddCamera(cam, DefinitionFile.Game.GraphicsDevice, DefinitionFile.Game.ResourceFactory);

            var collisionConf = new DefaultCollisionConfiguration();
            var dispatcher = new CollisionDispatcher(collisionConf);
            var broadphase = new DbvtBroadphase();
            var solver = new SequentialImpulseConstraintSolver();

            World = new DiscreteDynamicsWorld(dispatcher, broadphase, solver, collisionConf);
            World.SetGravity(ref Gravity);

            World.Broadphase.OverlappingPairCache.SetInternalGhostPairCallback(new GhostPairCallback());

            World.SetInternalTickCallback(SimulationStepped, null, true);
        }

        void SimulationStepped(DynamicsWorld world, float step)
        {
            foreach (var item in ActiveTriggers)
                item.HandleCollisions();
        }

        public void AddCamera(Camera cam, GraphicsDevice device, ResourceFactory factory)
        {
            Cameras.Add(cam);

            var buf = factory.CreateBuffer(
                new((uint)LightUploadScheme.Size, BufferUsage.Dynamic | BufferUsage.UniformBuffer)
            );

            LightBuffers[cam] = buf;
            LightSets[cam] = factory.CreateResourceSet(new ResourceSetDescription(LightLayout, buf));
        }

        public ResourceSet GetLightSet(Camera cam) => LightSets[cam];

        public unsafe void UploadLights(Camera cam, GraphicsDevice device)
        {
            var sorted = Lights
                .OrderByDescending(light =>
                {
                    float dist = Vector3.Distance(cam.Position, light.Position);
                    return light.Intensity / dist;
                })
                .Take(LightUploadScheme.MAX_POINT_LIGHTS)
                .ToList();

            LightUploadScheme scheme = new();
            scheme.PointLightCount = (uint)sorted.Count;
            for (int i = 0; i < sorted.Count; i++)
                scheme.Lights[i] = sorted[i].reference;

            scheme.Sun = Sun.reference;

            device.UpdateBuffer(LightBuffers[cam], 0, ref scheme);
        }

        public static void UploadLights(GraphicsDevice device)
        {
            foreach (var item in CurrentScene.Cameras)
                CurrentScene.UploadLights(item, device);
        }

        public static void SwitchScene(KumaScene scene,GraphicsDevice device,ResourceFactory factory)
        {
            scene.LightsDirty = true;
            CurrentScene = scene;
        }

        public void Dispose()
        {
            DefinitionFile.Game.GraphicsDevice.WaitForIdle();

            foreach (var item in LightSets)
                item.Value.Dispose();

            foreach (var item in LightBuffers)
                item.Value.Dispose();
        }
    }
}
