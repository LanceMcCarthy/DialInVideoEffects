using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Effects;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace DialIn.VideoEffects.Effects.Win2D
{
    public sealed class SaturationVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties currentEncodingProperties;
        private CanvasDevice canvasDevice;
        private IPropertySet configuration;

        private float Intensity
        {
            get
            {
                if (configuration != null && configuration.ContainsKey("Intensity"))
                    return (float)configuration["Intensity"];
                else
                    return 0.5f;
            }
            set
            {
                configuration["Intensity"] = value;
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            {
                var saturation = new SaturationEffect()
                {
                    Source = inputBitmap as IGraphicsEffectSource,
                    Saturation = this.Intensity
                };
                ds.DrawImage(saturation);
            }
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            currentEncodingProperties = encodingProperties;

            //TODO remove from original
            //_canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device, CanvasDebugLevel.Error);
            canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);
        }

        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }

        public bool IsReadOnly { get { return false; } }
        public MediaMemoryTypes SupportedMemoryTypes { get { return MediaMemoryTypes.Gpu; } }
        public bool TimeIndependent { get { return false; } }

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                return new List<VideoEncodingProperties>()
                {
                    // NOTE: Specifying width and height is only necessary due to bug in media pipeline when
                    // effect is being used with Media Capture. 
                    // This can be changed to "0, 0" in a future release of FBL_IMPRESSIVE. 
                    VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Argb32, 800, 600)
                };
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Clean up devices
            if (canvasDevice != null)
                canvasDevice.Dispose();
        }

        public void DiscardQueuedFrames()
        {
            // No cached frames to discard
        }
    }
}
