using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Storage.Streams;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using DialIn.VideoEffects.Uwp.Common;

namespace DialIn.VideoEffects.Uwp
{
    public sealed partial class MainPage : Page
    {
        #region Fields

        // General
        private RecordingState currentState = RecordingState.NotInitialized;
        private RadialController dialController;

        // Camera API fields
        private MediaCapture mediaCapture;
        private DeviceInformation selectedCamera;

        // Effect fields
        private IVideoEffectDefinition previewEffect;
        private IPropertySet effectPropertySet;
        
        #endregion
        
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Setup RadialController and add custom menu items
            ConfigureRadialController();

            // Set up preview video stream
            await InitializeVideoAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        { 
            base.OnNavigatedFrom(e);

            // Clean up RadialDial
            dialController.RotationChanged -= DialControllerRotationChanged;
            dialController?.Menu.Items.Clear();

            // Dispose camera
            await DisposeMediaCaptureAsync();
        }

        private void ConfigureRadialController()
        {
            // Setup DialController
            dialController = RadialController.CreateForCurrentView();
            dialController.RotationResolutionInDegrees = 1;

            // Wireup event handler for rotation
            dialController.RotationChanged += DialControllerRotationChanged;

            // Remove the default items to make more room for our custom items
            var config = RadialControllerConfiguration.GetForCurrentView();
            config.SetDefaultMenuItems(new[] { RadialControllerSystemMenuItemKind.Scroll });

            // Add a custom menu item for each of the video effects
            foreach (var effect in PageViewModel.VideoEffects)
            {
                // Create a menu item, using the effect's name and thumbnail
                var menuItem = RadialControllerMenuItem.CreateFromIcon(effect.DisplayName,
                    RandomAccessStreamReference.CreateFromUri(new Uri(effect.IconImagePath)));

                // Hook up it's invoked (aka selected) event handler
                menuItem.Invoked += MenuItem_Invoked;

                // Add it to the RadialDial
                dialController.Menu.Items.Add(menuItem);
            }
        }
        
        private async void MenuItem_Invoked(RadialControllerMenuItem sender, object args)
        {
            var selectedEffect = PageViewModel.VideoEffects.FirstOrDefault(e => e.DisplayName == sender?.DisplayText);
            
            if (selectedEffect == null)
                return;

            // If the selected effect was already selected, then remove the effect from the video stream
            if (selectedEffect == PageViewModel.SelectedEffect)
            {
                await ClearVideoEffectsAsync();
                PageViewModel.SelectedEffect = null;

                return;
            }

            // Set the currently selected effect
            PageViewModel.SelectedEffect = selectedEffect;

            // Clear previous effect
            await ClearVideoEffectsAsync();

            // Apply new effect
            await ApplyVideoEffectAsync();
        }
        
        private void DialControllerRotationChanged(RadialController sender, RadialControllerRotationChangedEventArgs args)
        {
            if (args == null)
                return;

            // Make sure we're still in range of the effect's property that we're changing
            if (SelectedEffectSlider.Value < PageViewModel.SelectedEffect.MinPropertyValue 
                || SelectedEffectSlider.Value > PageViewModel.SelectedEffect.MaxPropertyValue)
                return;
            
            SelectedEffectSlider.Value += args.RotationDeltaInDegrees / 100;

            UpdateEffect();
        }
        
        private void SelectedEffectSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateEffect();
        }

        /// <summary>
        /// Shared method to apply changes to the currently applied video effect
        /// </summary>
        private void UpdateEffect()
        {
            // Update Video Effect's values
            if (PageViewModel.SelectedEffect == null || effectPropertySet == null)
                return;

            // Update effect's values
            PageViewModel.SelectedEffect.PropertyValue = (float) SelectedEffectSlider.Value;
            effectPropertySet[PageViewModel.SelectedEffect.PropertyName] = (float) PageViewModel.SelectedEffect.PropertyValue;
        }
        
        #region Video effect selection, creation and management

        /// <summary>
        /// Creates a VideoEffectDefinition and it's corresponding property bag to pass the effects's values
        /// </summary>
        /// <returns>A video effect definition that can be applied to a video or photo stream</returns>
        private IVideoEffectDefinition ConstructVideoEffect()
        {
            if (string.IsNullOrEmpty(PageViewModel.SelectedEffect.VideoEffect.FullName))
                return null;
            
            if (string.IsNullOrEmpty(PageViewModel.SelectedEffect.PropertyName))
                return new VideoEffectDefinition(PageViewModel.SelectedEffect.VideoEffect.FullName);

            effectPropertySet = new PropertySet();
            effectPropertySet[PageViewModel.SelectedEffect.PropertyName] = PageViewModel.SelectedEffect.PropertyValue;

            return new VideoEffectDefinition(PageViewModel.SelectedEffect.VideoEffect.FullName, effectPropertySet);
        }

        /// <summary>
        /// Applies VideoEffectDefinition to the preview stream
        /// </summary>
        /// <returns></returns>
        private async Task ApplyVideoEffectAsync()
        {
            if (currentState == RecordingState.Previewing)
            {
                previewEffect = ConstructVideoEffect();
                await mediaCapture.AddVideoEffectAsync(previewEffect, MediaStreamType.VideoPreview);
            }
            else if (currentState == RecordingState.NotInitialized || currentState == RecordingState.Stopped)
            {
                await new MessageDialog("The preview or recording stream is not available.", "Effect not applied").ShowAsync();
            }
        }

        /// <summary>
        /// Clears effects from preview stream and sets preview effect to null
        /// </summary>
        /// <returns></returns>
        private async Task ClearVideoEffectsAsync()
        {
            await mediaCapture.ClearEffectsAsync(MediaStreamType.VideoPreview);
            previewEffect = null;
        }
        
        #endregion

        #region MediaCapture initialization and disposal

        private async Task InitializeVideoAsync()
        {
            ReloadVideoStreamButton.Visibility = Visibility.Collapsed;
            ShowBusyIndicator("Initializing...");

            try
            {
                currentState = RecordingState.NotInitialized;

                PreviewMediaElement.Source = null;

                ShowBusyIndicator("starting video device...");

                mediaCapture = new MediaCapture();
                App.MediaCaptureManager = mediaCapture;

                selectedCamera = await FindBestCameraAsync();

                if (selectedCamera == null)
                {
                    await new MessageDialog("There are no cameras connected, please connect a camera and try again.").ShowAsync();
                    await DisposeMediaCaptureAsync();
                    HideBusyIndicator();
                    return;
                }

                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = selectedCamera.Id });

                if (mediaCapture.MediaCaptureSettings.VideoDeviceId != "" && mediaCapture.MediaCaptureSettings.AudioDeviceId != "")
                {
                    ShowBusyIndicator("camera initialized..");
                    
                    mediaCapture.Failed += Failed;
                }
                else
                {
                    ShowBusyIndicator("camera error!");
                }
                
                //------starting preview----------//

                ShowBusyIndicator("starting preview...");

                PreviewMediaElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();

                currentState = RecordingState.Previewing;

            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"InitializeVideo UnauthorizedAccessException\r\n {ex}");

                ShowBusyIndicator("Unauthorized Access Error");

                await new MessageDialog("-----Unauthorized Access Error!-----\r\n\n" +
                                        "This can happen for a couple reasons:\r\n" +
                                        "-You have disabled Camera access to the app\r\n" +
                                        "-You have disabled Microphone access to the app\r\n\n" +
                                        "To fix this, go to Settings > Privacy > Camera (or Microphone) and reenable it.").ShowAsync();

                await DisposeMediaCaptureAsync();
            }
            catch (Exception ex)
            {
                ShowBusyIndicator("Initialize Video Error");
                await new MessageDialog("InitializeVideoAsync() Exception\r\n\nError Message: " + ex.Message).ShowAsync();

                currentState = RecordingState.NotInitialized;
                PreviewMediaElement.Source = null;
            }
            finally
            {
                HideBusyIndicator();
            }
        }
        
        private async Task DisposeMediaCaptureAsync()
        {
            try
            {
                ShowBusyIndicator("Freeing up resources...");

                if (currentState == RecordingState.Recording && mediaCapture != null)
                {
                    ShowBusyIndicator("recording stopped...");
                    await mediaCapture.StopRecordAsync();
                    
                }
                else if (currentState == RecordingState.Previewing && mediaCapture != null)
                {
                    ShowBusyIndicator("video preview stopped...");
                    await mediaCapture.StopPreviewAsync();
                }

                currentState = RecordingState.Stopped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisposeAll Error: {ex.Message}");
                await new MessageDialog($"Error disposing MediaCapture: {ex.Message}").ShowAsync();
            }
            finally
            {
                if (mediaCapture != null)
                {
                    mediaCapture.Failed -= Failed;
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }

                PreviewMediaElement.Source = null;
                HideBusyIndicator();
            }
        }

        private static async Task<DeviceInformation> FindBestCameraAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            
            Debug.WriteLine($"{devices.Count} devices found");
            
            // If there are no cameras connected to the device
            if (devices.Count == 0)
                return null;

            // If there is only one camera, return that one
            if (devices.Count == 1)
                return devices.FirstOrDefault();
            
            //check if the preferred device is available
            var frontCamera = devices.FirstOrDefault(
                x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);

            //if front camera is available return it, otherwise pick the first available camera
            return frontCamera ?? devices.FirstOrDefault();
        }
        
        private async void Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await TaskUtilities.RunOnDispatcherThreadAsync(async () =>
            {
                await new MessageDialog(currentFailure.Message, "MediaCaptureFailed Fired").ShowAsync();

                await DisposeMediaCaptureAsync();

                ReloadVideoStreamButton.Visibility = Visibility.Visible;
            });
        }
        
        private async void ReloadVideoStreamButton_OnClick(object sender, RoutedEventArgs e)
        {
            await InitializeVideoAsync();
        }

        #endregion

        #region Status messaging

        private void ShowBusyIndicator(string message)
        {
            PageViewModel.IsBusyMessage = message;
            PageViewModel.IsBusy = true;
        }

        private void HideBusyIndicator()
        {
            PageViewModel.IsBusyMessage = "";
            PageViewModel.IsBusy = false;
        }

        #endregion
    }
}
