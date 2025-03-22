using INotify.KToastView.Model;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastView.Control
{
    public class ToastHeaderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate KNotificationByPackageTemplate { get; set; }
        public DataTemplate KNotificationBySpaceTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is KNotificationByPackageCVS)
            {
                return KNotificationByPackageTemplate;
            }
            else if (item is KNotificationBySpaceCVS)
            {
                return KNotificationBySpaceTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }

    public class ToastItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate KToastTemplate { get; set; }
        public DataTemplate KPackageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is KToastVObj)
            {
                return KToastTemplate;
            }
            else if (item is KPackageProfileVObj)
            {
                return KPackageTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
