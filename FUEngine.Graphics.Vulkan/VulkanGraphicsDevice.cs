using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using FUEngine.Core.Graphics;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.GLFW;
using Silk.NET.Core.Native;
using VkImage = Silk.NET.Vulkan.Image;

namespace FUEngine.Graphics.Vulkan;

/// <summary>Implementación de IGraphicsDevice usando Vulkan. Soporta modo headless (Create) o con ventana GLFW y swapchain (CreateWithWindow).</summary>
public sealed class VulkanGraphicsDevice : IGraphicsDevice
{
    private readonly Vk _vk;
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private uint _graphicsQueueFamilyIndex;
    private bool _disposed;
    private float _clearR = 0.1f, _clearG = 0.1f, _clearB = 0.12f, _clearA = 1f;

    // Ruta con ventana y swapchain
    private Glfw? _glfw;
    private WindowHandle _window; // ventana GLFW (solo válida si _hasWindow)
    private SurfaceKHR _surface;
    private KhrSurface? _khrSurface;
    private KhrSwapchain? _khrSwapchain;
    private SwapchainKHR _swapchain;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private ImageView[] _swapchainImageViews = Array.Empty<ImageView>();
    private RenderPass _renderPass;
    private Framebuffer[] _framebuffers = Array.Empty<Framebuffer>();
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = Array.Empty<CommandBuffer>();
    private Queue _presentQueue;
    private uint _presentQueueFamilyIndex;
    private uint _currentImageIndex;
    private Extent2D _swapchainExtent;
    private Format _swapchainImageFormat;
    private bool _hasWindow;

    private VulkanGraphicsDevice() => _vk = Vk.GetApi();

    /// <summary>Crea un dispositivo Vulkan sin ventana (solo dispositivo y cola).</summary>
    public static VulkanGraphicsDevice? Create()
    {
        var device = new VulkanGraphicsDevice();
        try
        {
            device.CreateInstance(null);
            device.PickPhysicalDevice(null);
            device.CreateLogicalDevice(false);
            return device;
        }
        catch (Exception)
        {
            device.Dispose();
            return null;
        }
    }

    /// <summary>Crea un dispositivo Vulkan con ventana GLFW y swapchain; BeginFrame/EndFrame adquieren, limpian y presentan.</summary>
    public static VulkanGraphicsDevice? CreateWithWindow(int width, int height, string title = "FUEngine")
    {
        var device = new VulkanGraphicsDevice();
        try
        {
            device.InitGlfwAndWindow(width, height, title);
            var extensions = device.GetGlfwRequiredExtensions();
            device.CreateInstance(extensions);
            device.CreateSurface();
            device.PickPhysicalDevice(device._surface);
            device.CreateLogicalDevice(true);
            device.CreateSwapchain(width, height);
            device.CreateImageViews();
            device.CreateRenderPass();
            device.CreateFramebuffers();
            device.CreateCommandPool();
            device.CreateCommandBuffers();
            device._hasWindow = true;
            return device;
        }
        catch (Exception)
        {
            device.Dispose();
            return null;
        }
    }

    public bool IsValid => !_disposed && _device.Handle != 0;
    public int Width => _hasWindow ? (int)_swapchainExtent.Width : 0;
    public int Height => _hasWindow ? (int)_swapchainExtent.Height : 0;

    /// <summary>Puntero a la ventana GLFW (solo válido si se creó con CreateWithWindow).</summary>
    public unsafe nint WindowHandle => _hasWindow ? (nint)Unsafe.AsPointer(ref _window) : 0;

    public void SetClearColor(float r, float g, float b, float a = 1f)
    {
        _clearR = r; _clearG = g; _clearB = b; _clearA = a;
    }

    public void BeginFrame()
    {
        if (!_hasWindow || _swapchain.Handle == 0) return;
        unsafe
        {
            var swapchain = _swapchain;
            _khrSwapchain!.AcquireNextImage(_device, swapchain, ulong.MaxValue, default, default, ref _currentImageIndex);
            var cmd = _commandBuffers[_currentImageIndex];
            var beginInfo = new CommandBufferBeginInfo { Flags = CommandBufferUsageFlags.OneTimeSubmitBit };
            _vk.BeginCommandBuffer(cmd, in beginInfo);
            var clearVal = new ClearValue(new ClearColorValue(_clearR, _clearG, _clearB, _clearA));
            var passInfo = new RenderPassBeginInfo
            {
                RenderPass = _renderPass,
                Framebuffer = _framebuffers[_currentImageIndex],
                RenderArea = new Rect2D(default, _swapchainExtent),
                ClearValueCount = 1,
                PClearValues = &clearVal
            };
            _vk.CmdBeginRenderPass(cmd, in passInfo, SubpassContents.Inline);
            _vk.CmdEndRenderPass(cmd);
            _vk.EndCommandBuffer(cmd);
        }
    }

    public void Clear() { /* Se hace en el render pass de BeginFrame */ }

    public void EndFrame()
    {
        if (!_hasWindow || _swapchain.Handle == 0) return;
        unsafe
        {
            var cmd = _commandBuffers[_currentImageIndex];
            var submitInfo = new SubmitInfo
            {
                CommandBufferCount = 1,
                PCommandBuffers = &cmd
            };
            _vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, default);
            _vk.QueueWaitIdle(_graphicsQueue);
            var swapchain = _swapchain;
            var imageIndex = _currentImageIndex;
            var presentInfo = new PresentInfoKHR
            {
                SwapchainCount = 1,
                PSwapchains = &swapchain,
                PImageIndices = &imageIndex
            };
            _khrSwapchain!.QueuePresent(_presentQueue, ref presentInfo);
        }
    }

    public unsafe void Dispose()
    {
        if (_disposed) return;
        _vk.DeviceWaitIdle(_device);
        if (_hasWindow)
        {
            for (int i = 0; i < _commandBuffers.Length; i++)
            {
                var cb = _commandBuffers[i];
                _vk.FreeCommandBuffers(_device, _commandPool, 1, ref cb);
            }
            _vk.DestroyCommandPool(_device, _commandPool, null);
            foreach (var fb in _framebuffers)
                _vk.DestroyFramebuffer(_device, fb, null);
            _vk.DestroyRenderPass(_device, _renderPass, null);
            foreach (var iv in _swapchainImageViews)
                _vk.DestroyImageView(_device, iv, null);
            _khrSwapchain?.DestroySwapchain(_device, _swapchain, null);
            _khrSurface?.DestroySurface(_instance, _surface, null);
            if (_hasWindow)
            {
                fixed (WindowHandle* pWin = &_window)
                    _glfw!.DestroyWindow(pWin);
            }
        }
        _vk.DestroyDevice(_device, null);
        _vk.DestroyInstance(_instance, null);
        _disposed = true;
    }

    private unsafe void InitGlfwAndWindow(int width, int height, string title)
    {
        _glfw = Glfw.GetApi();
        if (!_glfw.Init())
            throw new InvalidOperationException("No se pudo inicializar GLFW.");
        _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
        fixed (WindowHandle* pWin = &_window)
        {
            _glfw.CreateWindow(width, height, title, null, pWin);
        }
    }

    private unsafe string[] GetGlfwRequiredExtensions()
    {
        uint count = 0;
        var ptr = _glfw!.GetRequiredInstanceExtensions(out count);
        if (ptr == null || count == 0) return Array.Empty<string>();
        var list = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var p = ptr[i];
            if (p != null)
                list.Add(Marshal.PtrToStringAnsi((nint)p) ?? "");
        }
        return list.ToArray();
    }

    private unsafe void CreateInstance(string[]? extensions)
    {
        var appName = (byte*)Marshal.StringToHGlobalAnsi("FUEngine");
        var engineName = (byte*)Marshal.StringToHGlobalAnsi("FUEngine");
        const uint version100 = (1u << 22) | (0u << 12) | 0;
        var appInfo = new ApplicationInfo
        {
            PApplicationName = appName,
            ApplicationVersion = version100,
            PEngineName = engineName,
            EngineVersion = version100,
            ApiVersion = Vk.Version12
        };

        byte** ppExt = null;
        uint extCount = 0;
        if (extensions != null && extensions.Length > 0)
        {
            var extPtrs = new List<nint>();
            foreach (var e in extensions)
            {
                if (string.IsNullOrEmpty(e)) continue;
                extPtrs.Add(Marshal.StringToHGlobalAnsi(e + "\0"));
            }
            extCount = (uint)extPtrs.Count;
            ppExt = (byte**)Marshal.AllocHGlobal(extPtrs.Count * sizeof(nint));
            for (int i = 0; i < extPtrs.Count; i++)
                ((nint*)ppExt)[i] = extPtrs[i];
        }

        try
        {
            var createInfo = new InstanceCreateInfo
            {
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = extCount,
                PpEnabledExtensionNames = ppExt
            };
            if (_vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
                throw new InvalidOperationException("No se pudo crear la instancia Vulkan.");
        }
        finally
        {
            Marshal.FreeHGlobal((nint)appName);
            Marshal.FreeHGlobal((nint)engineName);
            if (ppExt != null)
            {
                for (uint i = 0; i < extCount; i++)
                    Marshal.FreeHGlobal(((nint*)ppExt)[i]);
                Marshal.FreeHGlobal((nint)ppExt);
            }
        }
        if (extensions != null && extensions.Length > 0)
            _khrSurface = new KhrSurface(_vk.Context);
    }

    private unsafe void CreateSurface()
    {
        VkNonDispatchableHandle handle;
        fixed (WindowHandle* pWin = &_window)
        {
            var result = (Result)_glfw!.CreateWindowSurface(new VkHandle(_instance.Handle), pWin, null, &handle);
            if (result != Result.Success)
                throw new InvalidOperationException($"No se pudo crear la superficie Vulkan: {result}");
        }
        _surface = new SurfaceKHR(handle.Handle);
    }

    private unsafe void PickPhysicalDevice(SurfaceKHR? surface)
    {
        uint count = 0;
        _vk.EnumeratePhysicalDevices(_instance, ref count, null);
        if (count == 0)
            throw new InvalidOperationException("No se encontró ningún dispositivo físico Vulkan.");
        var devices = new PhysicalDevice[count];
        unsafe
        {
            fixed (PhysicalDevice* pDevices = devices)
            {
                _vk.EnumeratePhysicalDevices(_instance, ref count, pDevices);
            }
        }

        foreach (var dev in devices)
        {
            uint queueCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(dev, ref queueCount, null);
            var props = new QueueFamilyProperties[queueCount];
            unsafe
            {
                fixed (QueueFamilyProperties* pProps = props)
                {
                    _vk.GetPhysicalDeviceQueueFamilyProperties(dev, ref queueCount, pProps);
                }
            }

            uint graphicsIdx = uint.MaxValue;
            uint presentIdx = uint.MaxValue;
            for (uint i = 0; i < queueCount; i++)
            {
                if ((props[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                    graphicsIdx = i;
                if (surface.HasValue && _khrSurface != null)
                {
                    _khrSurface.GetPhysicalDeviceSurfaceSupport(dev, i, surface.Value, out var ok);
                    if (ok) presentIdx = i;
                }
            }
            if (graphicsIdx == uint.MaxValue) continue;
            if (surface.HasValue && presentIdx == uint.MaxValue) continue;
            _physicalDevice = dev;
            _graphicsQueueFamilyIndex = graphicsIdx;
            _presentQueueFamilyIndex = surface.HasValue ? presentIdx : graphicsIdx;
            return;
        }
        throw new InvalidOperationException("No se encontró un dispositivo físico Vulkan adecuado.");
    }

    private unsafe void CreateLogicalDevice(bool withSwapchain)
    {
        var queueFamilies = new HashSet<uint> { _graphicsQueueFamilyIndex, _presentQueueFamilyIndex };
        float priority = 1f;
        var queueInfos = new List<DeviceQueueCreateInfo>();
        foreach (var qf in queueFamilies)
        {
            queueInfos.Add(new DeviceQueueCreateInfo
            {
                QueueFamilyIndex = qf,
                QueueCount = 1,
                PQueuePriorities = &priority
            });
        }

        byte* swapchainExt = null;
        if (withSwapchain)
        {
            var name = "VK_KHR_swapchain";
            swapchainExt = (byte*)Marshal.StringToHGlobalAnsi(name);
        }

        fixed (DeviceQueueCreateInfo* pQueueInfos = queueInfos.ToArray())
        {
            var deviceCreateInfo = new DeviceCreateInfo
            {
                QueueCreateInfoCount = (uint)queueInfos.Count,
                PQueueCreateInfos = pQueueInfos,
                EnabledExtensionCount = withSwapchain ? 1u : 0,
                PpEnabledExtensionNames = withSwapchain ? &swapchainExt : null
            };
            if (_vk.CreateDevice(_physicalDevice, in deviceCreateInfo, null, out _device) != Result.Success)
                throw new InvalidOperationException("No se pudo crear el dispositivo lógico Vulkan.");
        }
        if (withSwapchain)
            Marshal.FreeHGlobal((nint)swapchainExt);

        _vk.GetDeviceQueue(_device, _graphicsQueueFamilyIndex, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_device, _presentQueueFamilyIndex, 0, out _presentQueue);
        if (withSwapchain)
            _khrSwapchain = new KhrSwapchain(_vk.Context);
    }

    private unsafe void CreateSwapchain(int width, int height)
    {
        _khrSurface!.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out var caps);
        uint imageCount = caps.MinImageCount + 1;
        if (caps.MaxImageCount > 0 && imageCount > caps.MaxImageCount)
            imageCount = caps.MaxImageCount;

        _swapchainExtent = caps.CurrentExtent.Width != 0xFFFFFFFF
            ? caps.CurrentExtent
            : new Extent2D(
                (uint)Math.Clamp(width, (int)caps.MinImageExtent.Width, (int)caps.MaxImageExtent.Width),
                (uint)Math.Clamp(height, (int)caps.MinImageExtent.Height, (int)caps.MaxImageExtent.Height));

        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref formatCount, (SurfaceFormatKHR*)null);
        var formats = new SurfaceFormatKHR[formatCount];
        var countSpan = MemoryMarshal.CreateSpan(ref formatCount, 1);
        KhrSurfaceOverloads.GetPhysicalDeviceSurfaceFormats(_khrSurface!, _physicalDevice, _surface, countSpan, formats.AsSpan());
        _swapchainImageFormat = formats[0].Format;
        foreach (var f in formats)
        {
            if (f.Format == Format.B8G8R8A8Srgb && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                _swapchainImageFormat = f.Format;
                break;
            }
        }

        var indices = new[] { _graphicsQueueFamilyIndex, _presentQueueFamilyIndex };
        var unique = indices.Distinct().ToArray();
        fixed (uint* pIndices = unique)
        {
            var createInfo = new SwapchainCreateInfoKHR
            {
                Surface = _surface,
                MinImageCount = imageCount,
                ImageFormat = _swapchainImageFormat,
                ImageColorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr,
                ImageExtent = _swapchainExtent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
                ImageSharingMode = unique.Length == 1 ? SharingMode.Exclusive : SharingMode.Concurrent,
                QueueFamilyIndexCount = (uint)unique.Length,
                PQueueFamilyIndices = pIndices,
                PreTransform = caps.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = PresentModeKHR.FifoKhr,
                Clipped = true
            };
            if (_khrSwapchain!.CreateSwapchain(_device, in createInfo, null, out _swapchain) != Result.Success)
                throw new InvalidOperationException("No se pudo crear el swapchain Vulkan.");
        }

        var countArr = new uint[] { 0 };
        fixed (uint* pCount = countArr)
        {
            _khrSwapchain.GetSwapchainImages(_device, _swapchain, pCount, (VkImage*)null);
        }
        imageCount = countArr[0];
        _swapchainImages = new VkImage[imageCount];
        countArr[0] = imageCount;
        fixed (uint* pCount = countArr)
        {
            _khrSwapchain!.GetSwapchainImages(_device, _swapchain, pCount, _swapchainImages.AsSpan());
        }
    }

    private unsafe void CreateImageViews()
    {
        _swapchainImageViews = new ImageView[_swapchainImages.Length];
        for (int i = 0; i < _swapchainImages.Length; i++)
        {
            var viewInfo = new ImageViewCreateInfo
            {
                Image = (VkImage)_swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapchainImageFormat,
                Components = default,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
            };
            if (_vk.CreateImageView(_device, in viewInfo, null, out _swapchainImageViews[i]) != Result.Success)
                throw new InvalidOperationException("No se pudo crear ImageView.");
        }
    }

    private unsafe void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = _swapchainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };
        var colorRef = new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal);
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorRef
        };
        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };
        var renderPassInfo = new RenderPassCreateInfo
        {
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };
        if (_vk.CreateRenderPass(_device, in renderPassInfo, null, out _renderPass) != Result.Success)
            throw new InvalidOperationException("No se pudo crear el RenderPass.");
    }

    private unsafe void CreateFramebuffers()
    {
        _framebuffers = new Framebuffer[_swapchainImageViews.Length];
        for (int i = 0; i < _swapchainImageViews.Length; i++)
        {
            var iv = _swapchainImageViews[i];
            var fbInfo = new FramebufferCreateInfo
            {
                RenderPass = _renderPass,
                AttachmentCount = 1,
                PAttachments = &iv,
                Width = _swapchainExtent.Width,
                Height = _swapchainExtent.Height,
                Layers = 1
            };
            if (_vk.CreateFramebuffer(_device, in fbInfo, null, out _framebuffers[i]) != Result.Success)
                throw new InvalidOperationException("No se pudo crear Framebuffer.");
        }
    }

    private unsafe void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = _graphicsQueueFamilyIndex
        };
        if (_vk.CreateCommandPool(_device, in poolInfo, null, out _commandPool) != Result.Success)
            throw new InvalidOperationException("No se pudo crear el CommandPool.");
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[_framebuffers.Length];
        var allocInfo = new CommandBufferAllocateInfo
        {
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)_commandBuffers.Length
        };
        if (VkOverloads.AllocateCommandBuffers(_vk, _device, MemoryMarshal.CreateReadOnlySpan(ref allocInfo, 1), _commandBuffers.AsSpan()) != Result.Success)
            throw new InvalidOperationException("No se pudieron asignar CommandBuffers.");
    }
}
