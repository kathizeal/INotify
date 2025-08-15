using INotify.KToastViewModel.ViewModel;
using INotify.KToastViewModel.ViewModelContract;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.DI;

namespace INotify.KToastDI
{
    internal class KToastDIServiceProvider : DotNetDIServiceProviderBase
    {
        private static readonly object _lockObject = new object();
        private static volatile bool _isInitialized = false;

        public static KToastDIServiceProvider Instance { get { return KToastDIServiceProviderSingleton.Instance; } }

        /// <summary>Initializes the <see cref="DIServiceProviderBase.Container"/> with module specific DI services</summary>
        private KToastDIServiceProvider()
        {
            InitializeServices();
        }

        void InitializeServices()
        {
            if (_isInitialized) return;

            lock (_lockObject)
            {
                if (_isInitialized) return;

                try
                {
                    IServiceCollection servicesCollection = new ServiceCollection();

                    servicesCollection.AddTransient<ToastViewModelBase, ToastViewModel>();
                    servicesCollection.AddTransient<KToastListVMBase, KToastListViewModel>();
                    servicesCollection.AddTransient<KSpaceViewModelBase, KSpaceViewModel>();
                    servicesCollection.AddTransient<AppSelectionViewModelBase, AppSelectionViewModel>();
                    servicesCollection.AddTransient<NotificationListVMBase, NotificationListVM>();
                    servicesCollection.AddTransient<FeedbackVMBase, FeedbackVM>();
                    
                    BuildServiceProvider(servicesCollection, false); // addDefaultServices is sent as false since UIDI doesn't require PasswordProvider, DBAdapter & NetworkAdapter instances

                    _isInitialized = true;
                    System.Diagnostics.Debug.WriteLine("KToastDIServiceProvider initialized successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing KToastDIServiceProvider: {ex.Message}");
                    throw; // Re-throw to maintain existing behavior
                }
            }
        }

        public new T GetService<T>()
        {
            if (!_isInitialized)
            {
                InitializeServices();
            }

            try
            {
                return base.GetService<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting service {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        #region KToastDIServiceProvider class

        private class KToastDIServiceProviderSingleton
        {
            // Explicit static constructor
            static KToastDIServiceProviderSingleton() { }

            //Marked as internal as it will be accessed from the enclosing class. It doesn't raise any problem, as the class itself is private.
            internal static readonly KToastDIServiceProvider Instance = new KToastDIServiceProvider();
        }

        #endregion KToastDIServiceProvider class
    }
}
