using INotifyLibrary.DBHandler;
using INotifyLibrary.DBHandler.Contract;
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
            BuildServiceProvider(services);
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
