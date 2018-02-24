using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using DialIn.VideoEffects.Effects.Common;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace DialIn.VideoEffects.Effects.Win2D
{
    public sealed class VignetteVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;

        private float Amount
        {
            get
            {
                if (_configuration != null && _configuration.ContainsKey("Amount"))
                    return (float)_configuration["Amount"];

                return 0.1f;
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            // If memory type is CPU, the frame is in  InputFrame.SoftwareBitmap. 
            // For GPU, the frame is in InputFrame.Direct3DSurface

            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var effect = new VignetteEffect
                    {
                        Source = inputBitmap,
                        Amount = this.Amount
                    };

                    ds.DrawImage(effect);
                }

                return;
            }
            
            if (context.InputFrame.Direct3DSurface == null)
            {
                byte[] inputFrameBytes = new byte[4 * context.InputFrame.SoftwareBitmap.PixelWidth * context.InputFrame.SoftwareBitmap.PixelHeight];
                context.InputFrame.SoftwareBitmap.CopyToBuffer(inputFrameBytes.AsBuffer());

                using (var inputBitmap = CanvasBitmap.CreateFromBytes(
                    _canvasDevice,
                    inputFrameBytes,
                    context.InputFrame.SoftwareBitmap.PixelWidth,
                    context.InputFrame.SoftwareBitmap.PixelHeight,
                    context.InputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat()))

                using (var renderTarget = new CanvasRenderTarget(
                    _canvasDevice,
                    context.OutputFrame.SoftwareBitmap.PixelWidth,
                    context.OutputFrame.SoftwareBitmap.PixelHeight,
                    (float)context.OutputFrame.SoftwareBitmap.DpiX,
                    context.OutputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat(),
                    CanvasAlphaMode.Premultiplied))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        var effect = new VignetteEffect
                        {
                            Source = inputBitmap,
                            Amount = this.Amount
                        };

                        ds.DrawImage(effect);
                    }
                }
            }
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _currentEncodingProperties = encodingProperties;
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public MediaMemoryTypes SupportedMemoryTypes => EffectConstants.SupportedMemoryTypes;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => EffectConstants.SupportedEncodingProperties;

        public void Close(MediaEffectClosedReason reason)
        {
            _canvasDevice?.Dispose();
        }

        public bool IsReadOnly => false;
        public bool TimeIndependent => false;
        public void DiscardQueuedFrames() { }
    }
}
