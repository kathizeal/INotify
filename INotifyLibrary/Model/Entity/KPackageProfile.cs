using INotifyLibrary.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    public class KPackageProfile : ObservableObject, IKPackageProfile
    {
        public required string PackageId { get ; set;}
        public required string PackageFamilyName { get ; set;}
        public required string AppDisplayName { get; set; }
        public required string AppDescription { get; set; }
        public string? LogoFilePath { get; set; }
        public KPackageProfile() { }
    }
}
