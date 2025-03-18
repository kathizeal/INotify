using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastView.Model
{
    public class KPackageProfileVObj : KPackageProfile
    {
        private BitmapImage _appIcon;
        public BitmapImage AppIcon
        {
            get { return _appIcon; }
            set { SetProperty(ref _appIcon, value); }
        }
       
    }
}
