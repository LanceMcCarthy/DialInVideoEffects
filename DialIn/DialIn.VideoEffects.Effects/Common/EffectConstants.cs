using System.Collections.Generic;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace DialIn.VideoEffects.Effects.Common
{
    public static class EffectConstants
    {
        public static MediaMemoryTypes SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu;

        public static IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>()
        {
            // Bug in media pipeline...
            // When IBasicVideoEffect is used with MediaCapture, I need to explicitly define width and height
            // This can be changed to "0, 0" in a future release of Windows 10
            VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, 800, 600),
            VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Argb32, 800, 600)
        };
    }
}
