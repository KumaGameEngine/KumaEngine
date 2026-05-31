using RmlUiNet;
using KumaEngine.API;
using KumaEngine.Rendering;
using KumaEngine.Rendering.VertexTypes;
using KumaEngine.UI;
using KumaEngine.Windowing;
using System.Numerics;
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
            Camera = new Camera(Window.Width, Window.Height,factory);
            Camera.Position = new Vector3(0,0,5);

            KumaScene.CreateLightBuffers(factory);

            KumaPipeline.SwapChains.Add("MainSwapchain", MainSwapchain.Framebuffer.OutputDescription);;

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

            DefinitionFile.Init(this);

            Window.Resized += () =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                foreach (var item in UIAPI.UISurfaceHandles)
                    item.Value.Resize(Window.Width,Window.Height);
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

            if (LightAPI.UpdateLights)
            {
                KumaScene.UploadLights(GraphicsDevice);
                LightAPI.UpdateLights = false;
            }

            CommandList.Begin();

            Camera.Update(CommandList);

            CommandList.SetFramebuffer(MainSwapchain.Framebuffer);
            CommandList.ClearColorTarget(0, RgbaFloat.White);
            CommandList.ClearDepthStencil(1f);

            foreach (var item in KumaScene.CurrentGameObjects)
            {
                CommandList.UpdateBuffer(item.Pipeline.ModelBuffer, 0, new RocketModelScheme(item.Transform));
                item.Pipeline.Draw(CommandList,item.Model,item.Material);
            }

            if (KumaScene.CurrentSkyboxMaterial != null) 
                SkyboxPipeline.Draw(CommandList,SkyboxModel, KumaScene.CurrentSkyboxMaterial);

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
