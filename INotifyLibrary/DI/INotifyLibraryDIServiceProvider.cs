using INotifyLibrary.DataManger;
using INotifyLibrary.DBHandler;
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.DI;

namespace INotifyLibrary.DI
{
    public class INotifyLibraryDIServiceProvider: DotNetDIServiceProviderBase
    {
        public static INotifyLibraryDIServiceProvider Instance { get { return LibraryDIServiceProviderSingleton.Instance; } }

        private INotifyLibraryDIServiceProvider()
        {
               
        }

        public void Initialize(IServiceCollection services)
        {
            services.AddSingleton<INotifyDBHandler, NotifyDBHandler>();
            services.AddSingleton<IGetKToastsDataManager, GetKToastDataManager>();
            services.AddSingleton<IUpdateKToastDataManager, UpdateKToastDataManager>();
            services.AddSingleton<IGetAllSpaceDataManager, GetAllSpaceDataManager>();
            services.AddSingleton<IGetAllKPackageProfilesDataManager, GetAllKPackageProfilesDataManager>();
            services.AddSingleton<IAddPackageToSpaceDataManager, AddPackageToSpaceDataManager>();
            services.AddSingleton<IGetPackageBySpaceDataManager, GetPackageBySpaceDataManager>();
            services.AddSingleton<IAddAppsToConditionDataManager, AddAppsToConditionDataManager>();
            services.AddSingleton<IGetNotificationsByConditionDataManager, GetNotificationsByConditionDataManager>();
            services.AddSingleton<ISubmitFeedbackDataManager, SubmitFeedbackDataManager>();
            services.AddSingleton<ISoundMappingDataManager, SoundMappingDataManager>();
            services.AddSingleton<IClearPackageNotificationsDataManager, ClearPackageNotificationsDataManager>();

            BuildServiceProvider(services, true); // Build the service provider with default services
        }

        #region DIServiceProviderSingleton Class
        private class LibraryDIServiceProviderSingleton
        {
            // Explicit static constructor 
            static LibraryDIServiceProviderSingleton() { }

            //Marked as internal as it will be accessed from the enclosing class. It doesn't raise any problem, as the class itself is private.
            internal static readonly INotifyLibraryDIServiceProvider Instance = new INotifyLibraryDIServiceProvider();
        }
        #endregion
    }
}
