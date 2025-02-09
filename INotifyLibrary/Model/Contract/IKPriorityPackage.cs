using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    internal interface IKPriorityPackage
    {
        Priority Priority { get; set; }
        string PackageId { get; set; }
    }
}
