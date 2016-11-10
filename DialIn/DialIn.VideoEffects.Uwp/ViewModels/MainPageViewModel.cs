using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Windows.ApplicationModel;
using Windows.Storage;
using DialIn.VideoEffects.Effects.Win2D;
using DialIn.VideoEffects.Uwp.Models;

namespace DialIn.VideoEffects.Uwp.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region fields

        private ObservableCollection<VideoEffectItemViewModel> videoEffects;
        private VideoEffectItemViewModel selectedEffect;
        private bool isBusy;
        private string isBusyMessage = "working...";

        #endregion

        public MainPageViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;
        }

        #region properties

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
            get { return selectedEffect; }
            set { SetProperty(ref selectedEffect, value); }
        }
        
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        public string IsBusyMessage
        {
            get { return isBusyMessage; }
            set { SetProperty(ref isBusyMessage, value); }
        }

        #endregion

        #region INPC

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = GetPropertyName(propertyExpression);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler eventHandler = PropertyChanged;

            eventHandler?.Invoke(this, e);
        }

        protected bool SetProperty<T>(ref T storage, T value, Expression<Func<T>> propertyExpression)
        {
            string propertyName = GetPropertyName(propertyExpression);
            return SetProperty<T>(ref storage, value, propertyName);
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected static string GetPropertyName<T>(Expression<Func<T, object>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            UnaryExpression unaryExpression = (UnaryExpression) propertyExpression.Body;

            if (unaryExpression.Operand.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("ArgumentException should be a Member Access Lambda Expression", nameof(propertyExpression));

            MemberExpression memberExpression = (MemberExpression) unaryExpression.Operand;
            return memberExpression.Member.Name;
        }

        private string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            if (propertyExpression.Body.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("ArgumentException should be a Member Access Lambda Expression", nameof(propertyExpression));

            MemberExpression memberExpression = (MemberExpression) propertyExpression.Body;
            return memberExpression.Member.Name;
        }

        #endregion
    }
}
