using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Entity
{
    public class KSpaceMapper
    {    
        public string Id { get; set; }
        public string SpaceId { get; set; }
        public string PackageId { get; set; }

        public KSpaceMapper()
        {
            Id = SpaceId + "_" + PackageId;
        }
    }
}
