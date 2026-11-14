using KumaEngine.API;
using KumaEngine.Audio;
using KumaEngine.OpenAL;
using KumaEngine.OpenAL.Native;
using KumaEngine.OpenAL.Source;
using KumaEngine.Rendering;
using KumaEngine.Rendering.VertexTypes;
using KumaEngine.UI;
using KumaEngine.Windowing;
using RmlUiNet;
using System.Numerics;
using System.Runtime;
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
        Model FullScreenQuadModel;

        public Game(ApplicationWindow window) : base(window) { }

        protected override void CreateResources(ResourceFactory factory)
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            DefinitionFile.Game = this;

            var dev = new ALDevice(null!);
            var ctx = dev.CreateContext([]);
            ctx.MakeCurrent();

            Camera._cameraProjViewBuffer = factory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<MatrixPair>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Camera._cameraProjViewInverseBuffer = factory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<MatrixPair>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            Camera._cameraPosBuffer = factory.CreateBuffer(
                new BufferDescription((uint)Unsafe.SizeOf<CameraInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            GraphicsList = factory.CreateCommandList();
            ComputeList = factory.CreateCommandList();

            GraphicsFence = factory.CreateFence(false);
            ComputeFence = factory.CreateFence(false);

            var fb = MainSwapchain.Framebuffer;

            KumaPass.SwapChains.Add("MainSwapchain", new KumaNativeSwapchain());

            Rml.SetRenderInterface(new KumaUiRenderInterface(
                this,
                KumaPipeline.FromSet(factory, "ui"),
                GraphicsList
            ));
            Rml.SetFileInterface(new KumaFileInterface());
            Rml.SetSystemInterface(new KumaSystemInterface());
            Rml.Initialise();

            foreach (var item in AssetRetriver.GetFiles(AssetKind.UI, "Fonts", "*.ttf", true, true))
                Rml.LoadFontFace(Path.Combine("Fonts", item));

            bool first = true;

            DefinitionFile.Init();

            Window.Resized += () =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                foreach (var item in KumaPass.SwapChains.Values) 
                    if (item != null) item.Resize(factory);
            };

            SkyboxPipeline = KumaPipeline.FromSet(factory, "skybox");
            FullScreenQuadModel = new Model()
            {
                Vertices = new()
                {
                    Vertices =
                    [
                        new Vector3(-1f, -1f, 0f),
                        new Vector3( 1f, -1f, 0f),
                        new Vector3(-1f,  1f, 0f),
                        new Vector3( 1f,  1f, 0f),
                    ],
                    UVWLayers = 
                    [
                        [
                            new Vector3(0f, 1f, 0f),
                            new Vector3(1f, 1f, 0f),
                            new Vector3(0f, 0f, 0f),
                            new Vector3(1f, 0f, 0f),
                        ]
                    ]
                },
                Indices =
                [
                    0, 1, 2,
                    2, 1, 3,
                ],
            };
        }

        protected override void Draw(float deltaSeconds)
        {
            GameAPI.GameUpdate(deltaSeconds);

            if (KumaScene.CurrentScene == null) return;

            KumaScene.CurrentScene.World.StepSimulation(deltaSeconds, 10);

            if (KumaScene.CurrentScene.LightsDirty)
            {
                KumaScene.UploadLights(GraphicsDevice);
                KumaScene.CurrentScene.LightsDirty = false;
            }

            GraphicsList.Begin();
            ComputeList.Begin();

            KumaPipeline.ResetFramebuffer();

            foreach (var item in KumaPass.SwapChains.Values)
            {
                if (item == null) continue;

                if (item.ClearColorIDS.Count == 0 && !item.ClearDepth) continue;

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
                var go = KumaScene.CurrentScene.GameObjects.Where(x => 
                    !(x.Model is null && x.Pipeline is null) && 
                    x.Pipeline == item
                ).ToArray();

                if (go.Length > 0)
                {
                    foreach (var camera in KumaScene.CurrentScene.Cameras)
                    {
                        if (!camera.Enabled) continue;

                        camera.Update(GraphicsList);
                        item.Draw(GraphicsList, ComputeList, camera, go);
                    }
                }
            }

            bool skybox = KumaScene.CurrentScene.SkyboxMat != null;

            foreach (var item in KumaScene.CurrentScene.Cameras)
            {
                if (!item.Enabled) continue;
                if (skybox && item.ClearMode == CameraClearMode.Skybox)
                    SkyboxPipeline.Draw(GraphicsList, item, FullScreenQuadModel, KumaScene.CurrentScene.SkyboxMat!);

                if (item.PostProcessingPipeline != null)
                    item.PostProcessingPipeline.Draw(GraphicsList, item, FullScreenQuadModel, null!);
            }

            foreach (var item in UIAPI.UISurfaceHandles)
            {
                if (!item.Value.Visible) continue;

                var fb = item.Value.Swapchain.Framebuffer ?? MainSwapchain.Framebuffer;
                KumaUiRenderInterface.RenderFb = fb;

                item.Value.Resize(fb.Width, fb.Height);

                item.Value.Update(deltaSeconds);
                item.Value.Render();
            }

            ComputeList.End();
            GraphicsDevice.SubmitCommands(ComputeList);

            GraphicsList.End();
            GraphicsDevice.SubmitCommands(GraphicsList);

            GraphicsDevice.SwapBuffers(MainSwapchain);
        }

        protected override void Closing()
        {

        }
    }
}
