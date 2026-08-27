using KumaEngine.API;
using KumaEngine.Rendering;
using KumaEngine.Rendering.VertexTypes;
using KumaEngine.UI;
using KumaEngine.Windowing;
using RmlUiNet;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace KumaEngine
{
    public class Game : KumaApplication
    {
        public CommandList GraphicsList { get; private set; }
        public CommandList ComputeList { get; private set; }
        public Fence GraphicsFence { get; private set; }
        public Fence ComputeFence { get; private set; }

        KumaPipeline SkyboxPipeline;
        Model SkyboxModel;
        public Game(ApplicationWindow window) : base(window) { }

        protected override void CreateResources(ResourceFactory factory)
        {
            DefinitionFile.Game = this;

            Camera._cameraProjViewBuffer = factory.CreateBuffer(
                new BufferDescription((uint)(Unsafe.SizeOf<Matrix4x4>() * 2), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Camera._cameraProjViewInverseBuffer = factory.CreateBuffer(
                new BufferDescription((uint)(Unsafe.SizeOf<Matrix4x4>() * 2), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Camera._cameraPosBuffer = factory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<CameraInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            KumaScene.CreateLightBuffers(factory);

            KumaPass.SwapChains.Add("MainSwapchain", null!);

            GraphicsList = factory.CreateCommandList();
            ComputeList = factory.CreateCommandList();

            GraphicsFence = factory.CreateFence(false);
            ComputeFence = factory.CreateFence(false);

            Rml.SetRenderInterface(new KumaUiRenderInterface(
                this,
                KumaPipeline.FromSet(factory, "ui"),
                GraphicsList
            ));
            Rml.SetSystemInterface(new KumaSystemInterface());
            Rml.Initialise();

            foreach (var item in Directory.GetFiles(Path.Combine("Data", "Fonts"), "*.ttf"))
                Rml.LoadFontFace(item);

            bool first = true;

            DefinitionFile.Init();

            Window.Resized += () =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                foreach (var item in UIAPI.UISurfaceHandles)
                    item.Value.Resize(Window.Width,Window.Height);

                foreach (var item in KumaPass.SwapChains.Values) 
                    if (item != null) item.Resize(factory);
            };

            SkyboxPipeline = KumaPipeline.FromSet(factory, "skybox");
            SkyboxModel = Model.Create(GraphicsDevice, factory,
            [
                new SimpleVertex(new Vector3(-1f, -1f, 0f)),
                new SimpleVertex(new Vector3( 1f, -1f, 0f)),
                new SimpleVertex(new Vector3(-1f,  1f, 0f)),
                new SimpleVertex(new Vector3( 1f,  1f, 0f)),
            ],
            [
                0, 1, 2,
                2, 1, 3
            ]);
        }

        protected override void Draw(float deltaSeconds)
        {
            GameAPI.GameUpdate(deltaSeconds);

            if (KumaScene.CurrentCamera == null) return;

            KumaScene.UploadLights(GraphicsDevice);

            GraphicsList.Begin();
            ComputeList.Begin();

            KumaScene.CurrentCamera.Update(GraphicsList);

            foreach (var item in KumaPass.SwapChains.Values)
            {
                if (item == null) continue;

                GraphicsList.SetFramebuffer(item.Framebuffer);

                for (int i = 0; i < item.ClearColorIDS.Count; i++)
                    if (item.ClearColorIDS[i]) GraphicsList.ClearColorTarget((uint)i, RgbaFloat.White);

                if (item.ClearDepth) GraphicsList.ClearDepthStencil(1f);
            }

            GraphicsList.SetFramebuffer(MainSwapchain.Framebuffer);
            GraphicsList.ClearColorTarget(0, RgbaFloat.White);
            GraphicsList.ClearDepthStencil(1f);

            foreach (var item in PipelineAPI.PipelineHandles.Values)
            {
                var go = KumaScene.CurrentGameObjects.Where(x => 
                    !(x.Model is null && x.Pipeline is null) && 
                    x.Pipeline == item
                ).ToArray();

                if (go.Length > 0) item.Draw(GraphicsList, ComputeList, go);
            }

            if (KumaScene.CurrentSkyboxMaterial != null)
                SkyboxPipeline.Draw(GraphicsList, SkyboxModel, KumaScene.CurrentSkyboxMaterial);

            foreach (var item in UIAPI.UISurfaceHandles)
            {
                item.Value.Update();
                item.Value.Render();
            }

            ComputeList.End();

            GraphicsDevice.SubmitCommands(ComputeList,ComputeFence);
            GraphicsDevice.WaitForFence(ComputeFence, 5000000000);
            GraphicsDevice.ResetFence(ComputeFence);

            GraphicsList.End();

            GraphicsDevice.SubmitCommands(GraphicsList,GraphicsFence);
            GraphicsDevice.WaitForFence(GraphicsFence, 5000000000);
            GraphicsDevice.ResetFence(GraphicsFence);

            GraphicsDevice.SwapBuffers(MainSwapchain);
        }

        protected override void Closing()
        {
            if (UIAPI.UISurfaceHandles.Count > 0) Rml.Shutdown();
        }
    }
}
