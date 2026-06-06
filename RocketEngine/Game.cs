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
        public CommandList CommandList { get; private set; }

        KumaPipeline SkyboxPipeline;
        Model SkyboxModel;
        public Game(ApplicationWindow window) : base(window) { }

        protected override void CreateResources(ResourceFactory factory)
        {
            DefinitionFile.Game = this;

            Camera._cameraProjViewBuffer = factory.CreateBuffer(
                new BufferDescription((uint)(Unsafe.SizeOf<Matrix4x4>() * 2), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Camera._cameraPosBuffer = factory.CreateBuffer(
                new BufferDescription((uint)(Unsafe.SizeOf<Vector3>() + 4), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            KumaScene.CreateLightBuffers(factory);

            KumaPass.SwapChains.Add("MainSwapchain", null!);

            CommandList = factory.CreateCommandList();

            Rml.SetRenderInterface(new KumaUiRenderInterface(
                this,
                KumaPipeline.FromSet(factory, "ui"),
                CommandList
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

            if (LightAPI.UpdateLights)
            {
                KumaScene.UploadLights(GraphicsDevice);
                LightAPI.UpdateLights = false;
            }

            CommandList.Begin();

            KumaScene.CurrentCamera.Update(CommandList);

            foreach (var item in KumaPass.SwapChains.Values)
            {
                if (item == null) continue;

                CommandList.SetFramebuffer(item.Framebuffer);

                for (int i = 0; i < item.ClearColorIDS.Count; i++)
                    if (item.ClearColorIDS[i]) CommandList.ClearColorTarget((uint)i, RgbaFloat.White);

                if (item.ClearDepth) CommandList.ClearDepthStencil(1f);
            }

            CommandList.SetFramebuffer(MainSwapchain.Framebuffer);
            CommandList.ClearColorTarget(0, RgbaFloat.White);
            CommandList.ClearDepthStencil(1f);

            foreach (var item in PipelineAPI.PipelineHandles.Values)
            {
                var go = KumaScene.CurrentGameObjects.Where(x => x.Pipeline == item).ToArray();

                if (go.Length > 0) item.Draw(CommandList, go);
            }

            if (KumaScene.CurrentSkyboxMaterial != null)
                SkyboxPipeline.Draw(CommandList, SkyboxModel, KumaScene.CurrentSkyboxMaterial);

            foreach (var item in UIAPI.UISurfaceHandles)
            {
                item.Value.Update();
                item.Value.Render();
            }

            CommandList.End();

            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.WaitForIdle();

            GraphicsDevice.SwapBuffers(MainSwapchain);
        }

        protected override void Closing()
        {
            if (UIAPI.UISurfaceHandles.Count > 0) Rml.Shutdown();
        }
    }
}
