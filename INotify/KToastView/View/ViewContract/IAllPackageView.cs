using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUI3Component.ViewContract;

namespace INotify.KToastView.View.ViewContract
{
    public interface IAllPackageView : IView
    {
        void SoundPackageUpdated();
        void Package1Fetched();
        void Package2Fetched();
    }
}
