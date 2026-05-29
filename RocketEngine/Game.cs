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

        public Context rmlContext;
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

            rmlContext = Rml.CreateContext("main", new((int)Window.Width, (int)Window.Height))!;

            bool first = true;

            Window.Resized += () =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                Rml.RemoveContext("main");
                rmlContext.Dispose();
                rmlContext = Rml.CreateContext("main", new((int)Window.Width, (int)Window.Height))!;

                if (UIAPI.CurrentDocument != "") UIAPI.ShowCurrentDocument();
            };

            DefinitionFile.Init(this);

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
            if (rmlContext == null)
                rmlContext = Rml.CreateContext("main", new((int)Window.Width, (int)Window.Height))!;

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

            rmlContext.Update();
            rmlContext.Render();

            CommandList.End();

            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.WaitForIdle();

            GraphicsDevice.SwapBuffers(MainSwapchain);
        }

        protected override void Closing()
        {
            Rml.Shutdown();
        }
    }
}
