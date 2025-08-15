using INotify.KToastDI;
using INotifyLibrary;
using INotifyLibrary.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLogger;
using WinSQLiteDBAdapter.Model.Entity;
using WinUI3Component;
using WinUI3Component.Util;

namespace INotify.Manager
{
    public class InitializationManager : InitializationManagerBase
    {
        #region Singleton

        private InitializationManager() { }

        public static InitializationManager Instance { get { return InitializationManagerSingleton.Instance; } }

        private class InitializationManagerSingleton
        {
            internal static InitializationManager Instance = new InitializationManager();

            static InitializationManagerSingleton() { }
        }

        #endregion

        private bool IsDIAlreadyInitialized;

        public override void InitializeDI()
        {
            IServiceCollection services = new ServiceCollection();
            LibraryServiceManager.InitializeDI(services);
            var di = KToastDIServiceProvider.Instance;
        }

        protected override async Task InitializeAppTheme()
        {
            await Task.CompletedTask;
        }

        protected override async Task InitializeLibraryServicesForUser()
        {
            Logger.Info(LogManager.GetCallerInfo(), "START: Initialize library services for user");
            var dbFolderPath = WinUI3CommonUtil.GetRootDBFolderPath();
            await LibraryServiceManager.IntializeDB(dbFolderPath, INotifyConstant.CurrentUser);
        }

        /// <summary>
        /// Public method to initialize library services for the current user
        /// </summary>
        public async Task InitializeLibraryServicesForCurrentUser()
        {
            await InitializeLibraryServicesForUser();
        }
    }
}
