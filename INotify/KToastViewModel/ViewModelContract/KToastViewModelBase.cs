using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinCommon.Util;
using Windows.UI.Core;
using WinLogger.Contract;
using WinLogger;
using Microsoft.UI.Dispatching;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class KToastViewModelBase : ObservableObject, ICleanup
    {
        #region Properties
        public Dictionary<string, BitmapImage> IconCache = new Dictionary<string, BitmapImage>();

        public readonly ILogger Logger = LogManager.GetLogger();
        public CancellationTokenSource cts { get; private set; }

        public DispatcherQueue DispatcherQueue { get; private set; }

        private string _OwnerZuid;
        public string OwnerZuid
        {
            get { return _OwnerZuid; }
            set { _OwnerZuid = value; OnPropertyChanged(); }
        }

        #endregion Properties

        #region Constructor

        public KToastViewModelBase()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            ResetCTS();
        }

        #endregion Constructor

        public void ResetCTS()
        {
            if (cts != null)
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
        }

        #region Virtual Methods

        public virtual void Dispose()
        {
        }

        protected virtual void RegisterNotification()
        {
        }
        protected virtual void UnRegisterNotification()
        {
        }

        protected virtual void ClearData()
        {

        }

        #endregion Virtual Methods
    }
}
