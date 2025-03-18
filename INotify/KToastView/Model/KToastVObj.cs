using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace INotify.KToastView.Model
{
	public class KToastVObj : KToastBObj
	{
		private BitmapImage _appIcon;
		public BitmapImage AppIcon
		{
			get { return _appIcon; }
			set { SetProperty(ref _appIcon, value); }
		}
        public KToastVObj(KToastNotification toastData, KPackageProfile packageProfile) : base(toastData, packageProfile) { }
    }
}
