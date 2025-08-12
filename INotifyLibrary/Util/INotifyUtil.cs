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
                PackageFamilyName = IKPackageProfileConstant.DefaultPackageFamilyName,
                AppDisplayName = IKPackageProfileConstant.DefaultAppDisplayName,
                AppDescription = IKPackageProfileConstant.DefaultAppDescription,
                LogoFilePath = IKPackageProfileConstant.DefaultLogoFilePath,
            };
        }

        public static KPackageProfile CreatePackageProfileForAllNotification()
        {
            return new KPackageProfile()
            {
                PackageFamilyName = IKPackageProfileConstant.DefaultAllInPackageIdFamilyName,
                AppDisplayName = IKPackageProfileConstant.DefaultAllInPackageIdDisplayName,
                AppDescription = IKPackageProfileConstant.DefaultAllInPackageIdDescription,
                LogoFilePath = IKPackageProfileConstant.DefaultAllInPackageIdLogoFilePath,
            };
        }

        public static KSpace GetDefaultSpace(string id, string name)
        {
            return new KSpace()
            {
                SpaceId = id,
                SpaceName = name,
                SpaceDescription = "Default WorkSpace",
                SpaceIconLogoPath = IKSpaceConstant.DefaultWorkSpaceIcon,
                IsDefaultWorkSpace = true
            };
        }

    }
}
