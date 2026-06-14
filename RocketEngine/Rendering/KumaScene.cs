using System.Numerics;
using Veldrid;

namespace KumaEngine.Rendering
{
    public class KumaScene
    {
        public static List<GameObject> CurrentGameObjects = new();
        public static KumaMaterial CurrentSkyboxMaterial = null!;
        public static List<KumaPointLight> CurrentPointLights = new(); 

        public static DeviceBuffer PointLightBuffer = null!;

        public static Camera CurrentCamera = null!;

        public List<GameObject> GameObjects = new();
        public KumaMaterial SkyboxMat = null!;
        public List<KumaPointLight> PointLights = new();
        public Camera Camera;

        public KumaScene(Camera cam)
        {
            Camera = cam;
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
            var sorted = CurrentPointLights
                .OrderByDescending(light =>
                {
                    float distSq = Vector3.DistanceSquared(CurrentCamera.Position, light.Position);
                    distSq = Math.Max(distSq, 0.0001f);
                    return (light.Intensity * light.Color.Length()) / distSq;
                })
                .Take(LightUploadScheme.MAX_POINT_LIGHTS)
                .ToList();

            LightUploadScheme scheme = new();
            scheme.PointLightCount = (uint)sorted.Count;
            for (int i = 0; i < sorted.Count; i++)
                scheme.PointLights[i] = sorted[i].reference;

            device.UpdateBuffer(PointLightBuffer, 0, ref scheme);
        }

        public static void SwitchScene(KumaScene scene,GraphicsDevice device,ResourceFactory factory)
        {
            CurrentSkyboxMaterial = scene.SkyboxMat;

            foreach (var item in CurrentGameObjects) item.Destroy();

            CurrentGameObjects.Clear();
            CurrentGameObjects.AddRange(scene.GameObjects);

            CurrentPointLights.Clear();
            CurrentPointLights.AddRange(scene.PointLights);

            CurrentCamera = scene.Camera;

            CreateLightBuffers(factory);
        }
    }
}
