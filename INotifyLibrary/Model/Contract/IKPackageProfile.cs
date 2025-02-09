using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    internal interface IKPackageProfile
    {
        string PackageId { get; set; }
        string PackageFamilyName { get; set; }
        string AppDisplayName { get; set; }
        string AppDescription { get; set; }
        string? LogoFilePath { get; set; }
    }
}
