using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace DialIn.VideoEffects.Effects.Helpers
{
    public static class EffectConstants
    {
        public static MediaMemoryTypes SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu;

        public static IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>()
        {
            // NOTE: Specifying width and height is only necessary due to bug in media pipeline when
            // effect is being used with Media Capture. 
            // This can be changed to "0, 0" in a future release of FBL_IMPRESSIVE. 
            VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, 800, 600),
            VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Argb32, 800, 600)
        };
    }
}
