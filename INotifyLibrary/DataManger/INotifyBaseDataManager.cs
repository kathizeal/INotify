using INotifyLibrary.DBHandler.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.DataManger
{
    public abstract class INotifyBaseDataManager
    {

        protected static INotifyDBHandler DBHandler { get; set; }

        public INotifyBaseDataManager(INotifyDBHandler dBHandler)
        {
            DBHandler = dBHandler;
        }

    }
}
