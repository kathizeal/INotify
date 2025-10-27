using SQLite;
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
        [PrimaryKey]
        public string Id { get; set; }
        public string SpaceId { get; set; }
        public string PackageFamilyName { get; set; }

        public KSpaceMapper()
        {
        }

        public KSpaceMapper(string spaceId, string packageFamilyName)
        {
            SpaceId = spaceId;
            PackageFamilyName = packageFamilyName;
            Id = SpaceId + "_" + PackageFamilyName;

        }
    }
}
