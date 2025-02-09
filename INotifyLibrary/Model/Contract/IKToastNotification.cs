using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    public interface IKToastNotification
    {
        string NotificationId { get; set; }
        string PackageId { get; set; }
        DateTimeOffset CreatedTime {get;set;}
        string NotificationTitle { get; set; }
        string NotificationMessage { get; set; }
        
    }
}
