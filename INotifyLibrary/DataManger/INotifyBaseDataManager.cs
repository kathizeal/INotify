using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.DataManger
{
    public abstract class INotifyBaseDataManager
    {

        protected static ConcurrentDictionary<string, KPackageProfile> PackageProfileCache;
        protected static INotifyDBHandler DBHandler { get; set; }

        public INotifyBaseDataManager(INotifyDBHandler dBHandler)
        {
            DBHandler = dBHandler;
            PackageProfileCache = new();
        }

    }
}
