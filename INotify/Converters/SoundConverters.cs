using INotify.Services;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Data;
using System;

namespace INotify.Converters
{
    /// <summary>
    /// Converter to map NotificationSounds enum to ComboBox selected index
    /// </summary>
    public class SoundToComboBoxIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is NotificationSounds sound)
            {
                return (int)sound;
            }
            return 0; // Default to "None" if no value
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is int index && Enum.IsDefined(typeof(NotificationSounds), index))
            {
                return (NotificationSounds)index;
            }
            return NotificationSounds.None; // Default fallback
        }
    }

    /// <summary>
    /// Converter to map NotificationSounds enum to display text
    /// Now supports both custom and system sounds
    /// </summary>
    public class SoundToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is NotificationSounds sound)
            {
                return NotificationSoundHelper.GetSoundDisplayText(sound);
            }
            return "Default";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to get sound type description (Custom/System/Default)
    /// </summary>
    public class SoundTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is NotificationSounds sound)
            {
                return NotificationSoundHelper.GetSoundTypeDescription(sound);
            }
            return "Default";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}