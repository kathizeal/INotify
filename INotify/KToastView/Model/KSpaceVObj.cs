using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;

namespace INotify.KToastView.Model
{
    public class KSpaceVObj : KSpace
    {
        private BitmapImage _appIcon;
        public BitmapImage AppIcon
        {
            get { return _appIcon; }
            set { SetProperty(ref _appIcon, value); }
        }
        public KSpaceVObj() : base() { }

        public void Update(KSpace kSpace)
        {
            base.Update(kSpace);
        }

    }
}
