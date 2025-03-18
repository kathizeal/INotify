using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotifyLibrary.Util
{
    public static class INotifyUtil
    {
        public static KPackageProfile GetDefaultPackageProfile()
        {
            return new KPackageProfile()
            {
                PackageId = IKPackageProfileConstant.DefaultPackageId,
                PackageFamilyName = IKPackageProfileConstant.DefaultPackageFamilyName,
                AppDisplayName = IKPackageProfileConstant.DefaultAppDisplayName,
                AppDescription = IKPackageProfileConstant.DefaultAppDescription,
                LogoFilePath = IKPackageProfileConstant.DefaultLogoFilePath,
                IsIconAvailable = IKPackageProfileConstant.DefaultIsIconAvailable
            };
        }
    }
}
