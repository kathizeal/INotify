using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    public interface IKSpace
    {
        string SpaceId { get; }
        string SpaceName { get;  }
        string SpaceDescription { get;}
        DateTimeOffset SpaceCreatedTime { get; }
        DateTimeOffset SpaceModifiedTime { get; }
    }
}
