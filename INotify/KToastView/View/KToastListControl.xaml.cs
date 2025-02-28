using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KToastListControl : UserControl
    {
        private KToastListVMBase _VM;
        public KToastListControl()
        {
            _VM = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
            this.InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void UpdateNotificationsList(ObservableCollection<KToastNotification> currentSystemNotifications)
        {
            _VM.UpdateKToastNotifications(currentSystemNotifications);
        }

        public void AddToastControl(KToastNotification notification)
        {
            _VM.KToastNotifications.Add(notification);
        }
    }
}
