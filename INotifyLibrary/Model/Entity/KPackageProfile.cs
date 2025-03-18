using INotifyLibrary.Model.Contract;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    public class KPackageProfile : ObservableObject, IKPackageProfile
    {
        private string _packageId;
        private string _packageFamilyName;
        private string _appDisplayName;
        private string _appDescription;
        private string? _logoFilePath;

        [PrimaryKey]
        public string PackageId
        {
            get => _packageId;
            set => SetProperty(ref _packageId, value);
        }

        public string PackageFamilyName
        {
            get => _packageFamilyName;
            set => SetProperty(ref _packageFamilyName, value);
        }

        public string AppDisplayName
        {
            get => _appDisplayName;
            set => SetProperty(ref _appDisplayName, value);
        }

        public string AppDescription
        {
            get => _appDescription;
            set => SetProperty(ref _appDescription, value);
        }

        public string? LogoFilePath
        {
            get => _logoFilePath;
            set
            {
                SetProperty(ref _logoFilePath, value);
                if (!string.IsNullOrWhiteSpace(_logoFilePath))
                {
                    IsIconAvailable = true;
                }
            }
        }


        [DefaultValue(false)]
        public bool IsIconAvailable { get; set; }
        [DefaultValue(false)]
        public bool IsIconOverride { get; set; }


        public KPackageProfile() { }

        public KPackageProfile DeepClone()
        {
            return new KPackageProfile
            {
                PackageId = this.PackageId,
                PackageFamilyName = this.PackageFamilyName,
                AppDisplayName = this.AppDisplayName,
                AppDescription = this.AppDescription,
                LogoFilePath = this.LogoFilePath,
                IsIconAvailable = this.IsIconAvailable,
                IsIconOverride = this.IsIconOverride
            };
        }

        public void Update(KPackageProfile newData)
        {
            if (newData == null) { return; }

            PackageId = newData.PackageId;
            PackageFamilyName = newData.PackageFamilyName;
            AppDisplayName = newData.AppDisplayName;
            AppDescription = newData.AppDescription;
            LogoFilePath = newData.LogoFilePath;
            IsIconAvailable = newData.IsIconAvailable;
            IsIconOverride = newData.IsIconOverride;
        }
    }
}
