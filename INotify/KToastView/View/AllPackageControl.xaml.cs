using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace INotify.View
{
    public sealed partial class AllPackageControl : UserControl , IAllPackageView
    {
        private ToastViewModelBase _VM;
        private SoundSelectionVMBase _SoundVM;
        private bool package1Fetched;
        private bool package2Fetched;
        private bool soundFetched;
        
        public AllPackageControl()
        {
            try
            {
                InitializeComponent();
                InitializeViewModel();
                GetAllApps();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AllPackageControl constructor: {ex.Message}");
            }
        }

        private void InitializeViewModel()
        {
            try
            {
                _VM = KToastDIServiceProvider.Instance.GetService<ToastViewModelBase>();
                _SoundVM = KToastDIServiceProvider.Instance.GetService<SoundSelectionVMBase>();
                
                if (_VM == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: ToastViewModelBase service not available from DI container");
                }
                else
                {
                    _VM.View = this;
                }

                if (_SoundVM == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: SoundSelectionVMBase service not available from DI container");
                }
                else
                {
                    _SoundVM.View = this;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ViewModel in AllPackageControl: {ex.Message}");
            }
        }

        private async void GetAllApps()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AllPackageControl: Starting to get all apps...");
                
                if (_VM != null)
                {
                    _VM.GetInstalledApps();
                    _VM.GetAppPackageProfile();
                    
                    System.Diagnostics.Debug.WriteLine("AllPackageControl: Called GetInstalledApps and GetAppPackageProfile");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AllPackageControl: ViewModel is null!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllApps: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refresh of app icons - useful for debugging
        /// </summary>
        public async void RefreshAppIcons()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AllPackageControl: Manually refreshing app icons...");
                
                if (_VM != null)
                {
                    // Force reload of apps with icons
                    _VM.GetInstalledApps();
                    
                    // Update the ListView binding
                    await Task.Delay(1000); // Give time for loading
                    
                    if (PackageListView != null)
                    {
                        PackageListView.ItemsSource = null;
                        PackageListView.ItemsSource = _VM.PackageProfiles;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"AllPackageControl: Refreshed {_VM.PackageProfiles.Count} packages");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshAppIcons: {ex.Message}");
            }
        }

        private void AppCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void SoundSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox comboBox && 
                    comboBox.Tag is KPackageProfileVObj packageProfile &&
                    comboBox.SelectedItem is ComboBoxItem selectedItem &&
                    _SoundVM != null)
                {
                    // Get the sound enum value from the selected item tag
                    if (Enum.TryParse<NotificationSounds>(selectedItem.Tag?.ToString(), out var sound))
                    {
                        // Update the sound for this package
                        _SoundVM.UpdatePackageSound(packageProfile.PackageFamilyName, sound);
                        
                        System.Diagnostics.Debug.WriteLine($"Sound changed for {packageProfile.DisplayName}: {sound}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SoundSelectionComboBox_SelectionChanged: {ex.Message}");
            }
        }

        private async void TestSoundButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && 
                    button.Tag is KPackageProfileVObj packageProfile &&
                    _SoundVM != null)
                {
                    // Get the current sound for this package
                    var currentSound = _SoundVM.GetPackageSound(packageProfile.PackageFamilyName);
                    
                    // Provide visual feedback
                    button.IsEnabled = false;
                    var originalOpacity = button.Opacity;
                    button.Opacity = 0.6;
                    
                    System.Diagnostics.Debug.WriteLine($"Testing sound for {packageProfile.DisplayName}: {currentSound}");
                    
                    // Test the sound
                    await _SoundVM.TestSoundAsync(currentSound);
                    
                    // Restore button state after a short delay
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(1000);
                        _SoundVM.DispatcherQueue.TryEnqueue(() =>
                        {
                            button.IsEnabled = true;
                            button.Opacity = originalOpacity;
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TestSoundButton_Click: {ex.Message}");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set initial sound selections based on saved mappings
                SetInitialSoundSelections();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UserControl_Loaded: {ex.Message}");
            }
        }

        private async void SetInitialSoundSelections()
        {
            try
            {
                if (_SoundVM == null) return;

                // Wait for ListView to be fully loaded and then set sound selections
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    _SoundVM.DispatcherQueue.TryEnqueue ( async() =>
                    {
                        PackageListView.ItemsSource = _VM.PackageProfiles;
                        SetSoundSelectionsForVisibleItems();
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetInitialSoundSelections: {ex.Message}");
            }
        }

        private void SetSoundSelectionsForVisibleItems()
        {
            try
            {
                if (_VM == null || _SoundVM == null) return;

                // Iterate through all items and set their sound selections
                foreach (var item in _VM.PackageProfiles)
                {
                    if (item is KPackageProfileVObj package)
                    {
                        var currentSound = _SoundVM.GetPackageSound(package.PackageFamilyName);
                        package.NotificationSounds = currentSound;
                        SetComboBoxSelection(package, currentSound);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetSoundSelectionsForVisibleItems: {ex.Message}");
            }
        }

        private void SetComboBoxSelection(KPackageProfileVObj package, NotificationSounds sound)
        {
            try
            {
                // Find the ListViewItem container for this package
                var container = PackageListView.ContainerFromItem(package) as FrameworkElement;
                if (container == null) return;

                // Find the ComboBox within the container
                var comboBox = FindChildByName<ComboBox>(container, "SoundSelectionComboBox");
                if (comboBox != null)
                {
                    // Set the selected item based on the sound
                    var targetTag = sound.ToString();
                    foreach (ComboBoxItem item in comboBox.Items)
                    {
                        if (item.Tag?.ToString() == targetTag)
                        {
                            comboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetComboBoxSelection: {ex.Message}");
            }
        }

        private T FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            try
            {
                if (parent == null) return null;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T element && element.Name == name)
                    {
                        return element;
                    }

                    var result = FindChildByName<T>(child, name);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FindChildByName: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
        }

        public void SoundPackageUpdated()
        {
            soundFetched = true; updateSound();
        }

        public void Package1Fetched()
        {
            package1Fetched = true; updateSound();
        }

        public void Package2Fetched()
        {
            package2Fetched = true; updateSound();
        }

        void updateSound()
        {
            if (package1Fetched && package2Fetched && soundFetched)
            {
                SetInitialSoundSelections();
            }
        }
    }
}
