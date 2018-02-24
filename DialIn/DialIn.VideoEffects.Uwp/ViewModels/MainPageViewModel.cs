using System.Collections.ObjectModel;
using Windows.ApplicationModel;
using DialIn.VideoEffects.Effects.Win2D;
using DialIn.VideoEffects.Uwp.Models;

namespace DialIn.VideoEffects.Uwp.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private ObservableCollection<VideoEffectItemViewModel> videoEffects;
        private VideoEffectItemViewModel selectedEffect;
        
        public MainPageViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;
        }
        
        public ObservableCollection<VideoEffectItemViewModel> VideoEffects => videoEffects ?? 
            (videoEffects = new ObservableCollection<VideoEffectItemViewModel>
               {
                   new VideoEffectItemViewModel(typeof(EdgeDetectionVideoEffect), "EdgeDetection", "Amount", 0.5f, 1f),
                   new VideoEffectItemViewModel(typeof(SaturationVideoEffect), "Saturation", "Intensity", 0.5f, 1f),
                   new VideoEffectItemViewModel(typeof(SepiaVideoEffect), "Sepia", "Intensity", 0.5f, 1f),
                   new VideoEffectItemViewModel(typeof(VignetteVideoEffect), "Vignette", "Amount", 0.5f, 1f)
               });

        public VideoEffectItemViewModel SelectedEffect
        {
            get => selectedEffect;
            set => SetProperty(ref selectedEffect, value);
        }
    }
}
