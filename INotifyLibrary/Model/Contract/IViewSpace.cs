using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    public interface IViewSpace
    {
        string SpaceId { get; set; }
        string SpaceName { get; set; }
        string SpaceDescription { get; set;}
        DateTimeOffset SpaceCreatedTime { get; set; }
        DateTimeOffset SpaceModifiedTime { get; set; }
    }
}
