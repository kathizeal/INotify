using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using INotifyLibrary.Util.Enums;

namespace INotify.Converters
{
    /// <summary>
    /// Converter that converts boolean to Visibility (true = Visible, false = Collapsed)
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool boolValue = false;
            
            if (value is bool directBool)
            {
                boolValue = directBool;
            }
            else if (value != null)
            {
                boolValue = true; // Non-null values are considered true
            }

            // Check for invert parameter
            bool invert = parameter?.ToString()?.ToLower() == "invert";
            
            if (invert)
                boolValue = !boolValue;
                
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                return invert ? !result : result;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter that shows element when count is zero (for empty states)
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that hides element when count is zero (for content lists)
    /// </summary>
    public class CountToReverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for expand/collapse icon based on boolean state
    /// </summary>
    public class ExpandCollapseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "\uE70D" : "\uE70E"; // ChevronDown : ChevronRight
            }
            return "\uE70E"; // Default to ChevronRight
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for notification count display
    /// </summary>
    public class NotificationCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count switch
                {
                    0 => "No notifications",
                    1 => "1 notification",
                    _ => $"{count} notifications"
                };
            }
            return "No notifications";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for notification count visibility (show only if > 0)
    /// </summary>
    public class NotificationCountVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that shows warning for apps with existing priority (for Priority flyouts)
    /// </summary>
    public class PriorityWarningVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Priority priority)
            {
                return priority != Priority.None ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for empty state text based on filter status
    /// </summary>
    public class EmptyStateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool hasActiveFilters)
            {
                return hasActiveFilters 
                    ? "No notifications match your current filters. Try adjusting your search criteria."
                    : "Notifications will appear here when they are received";
            }
            return "Notifications will appear here when they are received";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}