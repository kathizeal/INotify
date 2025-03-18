using INotifyLibrary.Util;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    public class KSpace : ObservableObject
    {
        public string SpaceId { get; set; }

        private string _SpaceName;

        public string SpaceName
        {
            get { return _SpaceName; }
            set => SetIfDifferent(ref _SpaceName, string.IsNullOrWhiteSpace(value) ? _SpaceName : value);
        }

        private string _SpaceDescription;

        public string SpaceDescription
        {
            get { return _SpaceDescription; }
            set { _SpaceDescription = value; }
        }

        public string SpaceIconLogoPath { get; set; }

        public bool IsDefaultWorkSpace { get; set; } = true;

        public void Update(KSpace other)
        {
            if (other == null) return;

            SpaceId = other.SpaceId;
            SpaceName = other.SpaceName;
            SpaceDescription = other.SpaceDescription;
            SpaceIconLogoPath = other.SpaceIconLogoPath;
            IsDefaultWorkSpace = other.IsDefaultWorkSpace;
        }

        public KSpace DeepClone()
        {
            return new KSpace
            {
                SpaceId = this.SpaceId,
                SpaceName = this.SpaceName,
                SpaceDescription = this.SpaceDescription,
                SpaceIconLogoPath = this.SpaceIconLogoPath,
                IsDefaultWorkSpace = this.IsDefaultWorkSpace
            };
        }
    }
}
