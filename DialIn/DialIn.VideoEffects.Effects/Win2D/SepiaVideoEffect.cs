using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Collections.Generic;

//Sepia docs: http://microsoft.github.io/Win2D/html/T_Microsoft_Graphics_Canvas_Effects_SepiaEffect.htm

namespace DialIn.VideoEffects.Effects.Win2D
{
    /// <summary>
    /// Win2D sepia effect. 
    /// Intensity minvalue is 0f, maximum is 1f, default is 0.5f
    /// </summary>
    public sealed class SepiaVideoEffect : IBasicVideoEffect
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

                var sepia = new SepiaEffect
                {
                    Source = inputBitmap,
                    Intensity = this.Intensity
                };

                ds.DrawImage(sepia);
            }
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            currentEncodingProperties = encodingProperties;
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
                return new List<VideoEncodingProperties>();
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Clean up devices
            canvasDevice?.Dispose();
        }

        public void DiscardQueuedFrames()
        {
            // No cached frames to discard
        }
    }
}
