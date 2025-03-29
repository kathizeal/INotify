using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Model.Contract
{
    internal interface IKPackageProfile
    {
        string PackageId { get; }
        string PackageFamilyName { get; }
        string AppDisplayName { get; }
        string AppDescription { get; }
        string LogoFilePath { get; }
    }
}
