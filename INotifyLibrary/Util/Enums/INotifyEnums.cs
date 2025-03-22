using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Util.Enums
{
   public enum Priority
    {
        None, // default
        High,
        Medium,
        Low,
    }

    public enum ViewType
    {
        All = 0,
        Space,
        Package,
        Priority,
        Filters
    }

    public enum NotificatioRequestType
    { 
        ALL,
        Individual
    }



}
