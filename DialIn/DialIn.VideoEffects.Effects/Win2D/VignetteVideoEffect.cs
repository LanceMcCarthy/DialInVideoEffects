using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace DialIn.VideoEffects.Effects.Win2D
{
    public sealed class VignetteVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties currentEncodingProperties;
        private CanvasDevice canvasDevice;
        private IPropertySet configuration;

        private float Amount
        {
            get
            {
                if (configuration != null && configuration.ContainsKey("Amount"))
                    return (float)configuration["Amount"];

                return 0.1f;
            }
            set
            {
                configuration["Amount"] = value;
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            {

                var effect = new VignetteEffect
                {
                    Source = inputBitmap,
                    Amount = this.Amount
                };

                ds.DrawImage(effect);
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

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>();

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
