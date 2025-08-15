using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.View
{
    public sealed partial class AllPackageControl : UserControl
    {
        private ToastViewModelBase _VM;
        
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
                if (_VM == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: ToastViewModelBase service not available from DI container");
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
                if (_VM != null)
                {
                    _VM.GetInstalledApps();
                    _VM.GetAppPackageProfile();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllApps: {ex.Message}");
            }
        }

        private void AppCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }
    }
}
