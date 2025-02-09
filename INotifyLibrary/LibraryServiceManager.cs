using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLogger.Contract;
using WinLogger;
using Microsoft.Extensions.DependencyInjection;
using INotifyLibrary.DI;
using INotifyLibrary.DBHandler.Contract;
using WinSQLiteDBAdapter.Contract;

namespace INotifyLibrary
{
    public static class LibraryServiceManager
    {
        static ILogger Logger = LogManager.GetLogger();
        public static void InitializeDI(IServiceCollection serviceCollection)
        {
            INotifyLibraryDIServiceProvider.Instance.Initialize(serviceCollection);
        }

        public static  async Task IntializeDB(string dbFolderPath, string userId, string dbRefId = default)
        {
            INotifyDBHandler dBHandler = INotifyLibraryDIServiceProvider.Instance.GetService<INotifyDBHandler>();
            Logger.Info(LogManager.GetCallerInfo(), "Initializing DB for Service");
            await dBHandler.InitializeDBAsync(dbFolderPath, userId, dbRefId).ConfigureAwait(false);
            Logger.Info(LogManager.GetCallerInfo(), "Initialized DB for Service");
        }
    }
}
