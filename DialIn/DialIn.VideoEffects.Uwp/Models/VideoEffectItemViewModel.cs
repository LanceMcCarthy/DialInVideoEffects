using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace DialIn.VideoEffects.Uwp.Models
{
    public class VideoEffectItemViewModel : INotifyPropertyChanged
    {
        #region fields

        private Type videoEffect;
        private string displayName;
        private float minPropertyValue;
        private float maxPropertyValue;
        private object propertyValue;
        private string propertyName;
        private string iconImagePath;
        private bool isSelected;
        private bool isSliderEnabled;
        private bool isToggleEnabled;
        private Visibility sliderVisiblility = Visibility.Collapsed;
        private Visibility toggleVisibility = Visibility.Collapsed;

        #endregion

        #region constructors

        public VideoEffectItemViewModel() { }

        /// <summary>
        /// Model for real-time video effects. 
        /// </summary>
        /// <param name="effect">Needs to be custom IBasicVideoEffect</param>
        /// <param name="effectName">Name sets both display name AND icon file name</param>
        public VideoEffectItemViewModel(Type effect, string effectName)
        {
            videoEffect = effect;
            displayName = effectName;
            iconImagePath = $"ms-appx:///Images/{effectName}.jpg";
        }

        /// <summary>
        /// Model for real-time video effects using a Toggle for properties. 
        /// </summary>
        /// <param name="effect">Needs to be custom IBasicVideoEffect</param>
        /// <param name="effectName">Name sets both display name AND icon file name</param>
        ///  /// <param name="propertyName">The effect's alterable property name (i.e. Intensity, Amount or Angle)</param>
        /// /// <param name="defaultValue">Default Value for Toggle Switch</param>
        public VideoEffectItemViewModel(Type effect, string effectName, string propertyName, bool defaultValue)
        {
            videoEffect = effect;
            displayName = effectName;
            iconImagePath = $"ms-appx:///Images/{effectName}.jpg";
            this.propertyName = propertyName;
            propertyValue = defaultValue;
            isToggleEnabled = true;
        }
        
        /// <summary>
        /// Model for real-time video effects using a Slider for properties.
        /// </summary>
        /// <param name="effect">Needs to be custom IBasicVideoEffect</param>
        /// <param name="effectName">Name sets both display name AND icon file name</param>
        /// <param name="propertyName">The effect's alterable property (i.e. Intensity, Amount or Angle)</param>
        /// <param name="defaultValue">Default Value for Slider</param>
        /// <param name="minValue">Minimum Property Value</param>
        /// <param name="maxValue">Maximum Property Value</param>
        public VideoEffectItemViewModel(Type effect, string effectName, string propertyName, float defaultValue, float maxValue, float minValue = 0f)
        {
            videoEffect = effect;
            displayName = effectName;
            iconImagePath = $"ms-appx:///Images/{effectName}.jpg";
            this.propertyName = propertyName;
            propertyValue = defaultValue;
            maxPropertyValue = maxValue;
            minPropertyValue = minValue;
            isSliderEnabled = true;
        }

        #endregion

        #region Properties

        public Type VideoEffect
        {
            get { return videoEffect; }
            set { videoEffect = value; OnPropertyChanged(nameof(VideoEffect)); }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public float MinPropertyValue
        {
            get { return minPropertyValue; }
            set { minPropertyValue = value; OnPropertyChanged(nameof(MinPropertyValue)); }
        }

        public float MaxPropertyValue
        {
            get { return maxPropertyValue; }
            set { maxPropertyValue = value; OnPropertyChanged(nameof(MaxPropertyValue)); }
        }

        public object PropertyValue
        {
            get { return propertyValue; }
            set { propertyValue = value; OnPropertyChanged(nameof(PropertyValue)); }
        }

        public string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; OnPropertyChanged(nameof(PropertyName)); }
        }

        public string IconImagePath
        {
            get { return iconImagePath; }
            set { iconImagePath = value; OnPropertyChanged(nameof(IconImagePath)); }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public bool IsSliderEnabled
        {
            get { return isSliderEnabled; }
            set { isSliderEnabled = value; OnPropertyChanged(nameof(IsSliderEnabled)); }
        }

        public Visibility SliderVisiblility
        {
            get { return sliderVisiblility; }
            set { sliderVisiblility = value; OnPropertyChanged(nameof(SliderVisiblility)); }
        }

        public bool IsToggleEnabled
        {
            get { return isToggleEnabled; }
            set { isToggleEnabled = value; OnPropertyChanged(nameof(IsToggleEnabled)); }
        }

        public Visibility ToggleVisibility
        {
            get { return toggleVisibility; }
            set { toggleVisibility = value; OnPropertyChanged(nameof(ToggleVisibility)); }
        }

        #endregion

        #region INPC

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
