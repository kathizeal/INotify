using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    public interface IKPriorityPackage
    {
        Priority Priority { get; }
        string PackageId { get; }
    }
}
