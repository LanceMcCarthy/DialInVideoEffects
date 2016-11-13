using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace DialIn.VideoEffects.Uwp
{
    public sealed partial class EditPhotoPage : Page
    {
        private RadialController dialController;
        private string imageFilePath;
        CanvasVirtualBitmap virtualBitmap;

        public EditPhotoPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            currentView.BackRequested += CurrentView_BackRequested;

            if (e.Parameter != null)
                imageFilePath = e.Parameter as string;
            
            // Setup RadialController and add custom menu items
            ConfigureRadialController();
        }

        private async void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            try
            {
                ShowBusyIndicator("deleting temporary file...");

                // Delete temp file
                var tempFile = await StorageFile.GetFileFromPathAsync(imageFilePath);
                await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception deleting tempFile: {ex}");
            }
            finally
            {
                HideBusyIndicator();

                // Go back to main page
                if (Frame.CanGoBack)
                    Frame.GoBack();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            currentView.BackRequested -= CurrentView_BackRequested;

            // Dispose Win2D canvas
            MyCanvas.RemoveFromVisualTree();
            MyCanvas = null;

            base.OnNavigatedFrom(e);
        }

        private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (PageViewModel?.SelectedEffect == null)
                return;
            
            var cl = new CanvasCommandList(sender);

            using (var clds = cl.CreateDrawingSession())
            {
                switch (PageViewModel.SelectedEffect.DisplayName)
                {
                    case "EdgeDetection":
                        clds.DrawImage(new EdgeDetectionEffect { Source = cl, Amount = (float) SelectedEffectSlider.Value });
                        break;
                    case "Saturation":
                        clds.DrawImage(new SaturationEffect { Source = cl, Saturation = (float) SelectedEffectSlider.Value });
                        break;
                    case "Sepia":
                        clds.DrawImage(new SepiaEffect { Source = cl, Intensity = (float) SelectedEffectSlider.Value });
                        break;
                    case "Vignette":
                        clds.DrawImage(new VignetteEffect { Source = cl, Amount = (float) SelectedEffectSlider.Value });
                        break;
                    default:
                        break;
                }
            }
        }

        private void ConfigureRadialController()
        {
            // Setup DialController
            dialController = RadialController.CreateForCurrentView();
            dialController.RotationResolutionInDegrees = 1;

            // Wireup event handler for rotation
            dialController.RotationChanged += DialController_RotationChanged;
            dialController.ButtonClicked += DialController_ButtonClicked;

            // Remove the default items to make more room for our custom items
            var config = RadialControllerConfiguration.GetForCurrentView();

            // I want an undo option for this page
            config.SetDefaultMenuItems(new[] { RadialControllerSystemMenuItemKind.UndoRedo, RadialControllerSystemMenuItemKind.Scroll });

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

        private void MenuItem_Invoked(RadialControllerMenuItem sender, object args)
        {
            if (sender?.DisplayText == "Undo")
            {
                //TODO reset image to tempFile
                return;
            }

            var selectedEffect = PageViewModel.VideoEffects.FirstOrDefault(e => e.DisplayName == sender?.DisplayText);

            if (selectedEffect == null)
                return;

            // If the selected effect was already selected, then remove the effect from the video stream
            if (selectedEffect == PageViewModel.SelectedEffect)
            {
                PageViewModel.SelectedEffect = null;
                return;
            }

            // Set the currently selected effect
            PageViewModel.SelectedEffect = selectedEffect;
        }

        private void DialController_RotationChanged(RadialController sender, RadialControllerRotationChangedEventArgs args)
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

        private void UpdateEffect()
        {
            // Update Video Effect's values
            if (PageViewModel.SelectedEffect == null)
                return;

            MyCanvas.Invalidate();
        }

        private void MyCanvas_OnCreateResources(CanvasVirtualControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(LoadVirtualBitmap().AsAsyncAction());
        }

        private async void DialController_ButtonClicked(RadialController sender, RadialControllerButtonClickedEventArgs args)
        {
            try
            {
                ShowBusyIndicator("saving file...");

                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("Jpegs", new List<string>() {".jpg"});

                var file = await picker.PickSaveFileAsync();
                if (file == null)
                    return;

                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // watermark the image
                    var device = CanvasDevice.GetSharedDevice();

                    var bounds = virtualBitmap.Bounds;

                    var text = new CanvasCommandList(device);
                    using (var ds = text.CreateDrawingSession())
                    {
                        ds.DrawText("Created by Dial-In", bounds, Colors.White,
                            new CanvasTextFormat
                            {
                                VerticalAlignment = CanvasVerticalAlignment.Bottom,
                                HorizontalAlignment = CanvasHorizontalAlignment.Right,
                                FontFamily = "Segoe UI",
                                FontSize = (float) (bounds.Height/12)
                            });
                    }

                    var effect = new BlendEffect()
                    {
                        Background = virtualBitmap,
                        Foreground = text,
                        Mode = BlendEffectMode.Difference
                    };

                    // save the image to final file
                    await CanvasImage.SaveAsync(effect, bounds, 96, device, stream, CanvasBitmapFileFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Something went wrong saving the image: {ex}", "Exception").ShowAsync();
            }
            finally
            {
                HideBusyIndicator();
            }
        }

        private async Task LoadVirtualBitmap()
        {
            try
            {
                ShowBusyIndicator("loading image...");

                if (virtualBitmap != null)
                {
                    virtualBitmap.Dispose();
                    virtualBitmap = null;
                }

                if (!string.IsNullOrEmpty(imageFilePath))
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(imageFilePath);
                    using (var stream = await storageFile.OpenReadAsync())
                    {
                        virtualBitmap = await CanvasVirtualBitmap.LoadAsync(MyCanvas.Device, stream);
                    }
                }

                // This can happen if the page is unloaded while this method is running
                if (MyCanvas == null || virtualBitmap == null)
                    return;

                MyCanvas.Width = virtualBitmap.Size.Width;
                MyCanvas.Height = virtualBitmap.Size.Height;
                MyCanvas.Invalidate();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Something went wrong loading the image: {ex}", "Exception").ShowAsync();
            }
            finally
            {
                HideBusyIndicator();
            }
        }

        private void MyCanvas_RegionsInvalidated(CanvasVirtualControl sender, CanvasRegionsInvalidatedEventArgs args)
        {
            foreach (var region in args.InvalidatedRegions)
            {
                using (var ds = MyCanvas.CreateDrawingSession(region))
                {
                    if (virtualBitmap != null)
                        ds.DrawImage(virtualBitmap, region, region);
                }
            }
        }

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