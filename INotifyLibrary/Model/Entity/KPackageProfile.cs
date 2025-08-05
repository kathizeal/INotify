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
        private string _appDisplayName;
        private string _appDescription;
        private string _logoFilePath;

        [PrimaryKey]
        public string PackageFamilyName { get;  set; }

        public string AppDisplayName
        {
            get => _appDisplayName;
            set => SetIfDifferent(ref _appDisplayName, value);
        }

        public string AppDescription
        {
            get => _appDescription;
            set => SetIfDifferent(ref _appDescription, value);
        }

        public string LogoFilePath
        {
            get => _logoFilePath;
            set
            {
                SetIfDifferent(ref _logoFilePath, value);
                IsIconAvailable = !string.IsNullOrWhiteSpace(_logoFilePath);
            }
        }

        [Ignore]
        public bool IsIconAvailable { get; private set; }
        public bool IsIconOverride { get; set; }


        public KPackageProfile() { }

        public KPackageProfile DeepClone()
        {
            return new KPackageProfile
            {
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

            PackageFamilyName = newData.PackageFamilyName;
            AppDisplayName = newData.AppDisplayName;
            AppDescription = newData.AppDescription;
            LogoFilePath = newData.LogoFilePath;
            IsIconAvailable = newData.IsIconAvailable;
            IsIconOverride = newData.IsIconOverride;
        }
    }
}
