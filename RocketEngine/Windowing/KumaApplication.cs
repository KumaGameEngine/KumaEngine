/*
    Taken from: https://github.com/mellinoe/veldrid-samples/tree/master/src/SampleBase
    Modified by: RocketEngine Team
*/

using KumaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Veldrid;

namespace KumaEngine.Windowing
{
    public abstract class KumaApplication
    {
        public ApplicationWindow Window { get; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain MainSwapchain { get; private set; }

        public KumaApplication(ApplicationWindow window)
        {
            Window = window;
            Window.Resized += HandleWindowResize;
            Window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
            Window.GraphicsDeviceDestroyed += OnDeviceDestroyed;
            Window.Rendering += Draw;
            Window.Closing += Closing;
        }

        public void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            GraphicsDevice = gd;
            ResourceFactory = factory;
            MainSwapchain = sc;
            CreateResources(factory);
            CreateSwapchainResources(factory);
        }

        protected virtual void OnDeviceDestroyed()
        {
            GraphicsDevice = null;
            ResourceFactory = null;
            MainSwapchain = null;
        }

        protected abstract void CreateResources(ResourceFactory factory);

        protected virtual void CreateSwapchainResources(ResourceFactory factory) { }

        protected abstract void Draw(float deltaSeconds);
        protected abstract void Closing();

        protected virtual void HandleWindowResize()
        {
            
        }
    }
}
