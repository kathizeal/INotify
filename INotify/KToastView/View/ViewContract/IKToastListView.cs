using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastView.View.ViewContract
{
    public interface IKToastListView
    {
        CollectionViewSource KToastCollectionViewSource { get; }
        ListView KToastListView { get; }
        DataTemplate ToastTemplate { get; }
        DataTemplate NotificationByPackageTemplate { get; }
        DataTemplate NotificationBySpaceTemplate { get; }
        DataTemplate PackageTemplate { get; }
        DataTemplate SpaceTemplate { get; }
    }
}
