using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    public interface IKToastNotification
    {
        string NotificationId { get;  }
        string PackageFamilyName { get; }
        DateTimeOffset CreatedTime { get; }
        string NotificationTitle { get; }
        string NotificationMessage { get; }
        
    }
}
