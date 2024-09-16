using AwqatSalaat.Properties;
using System.Windows;

namespace AwqatSalaat.Helpers
{
    internal static class MessageBoxEx
    {
        #region Info
        public static MessageBoxResult Info(string message)
        {
            return Info(message, MessageBoxButton.OK);
        }

        public static MessageBoxResult Info(string message, MessageBoxButton button)
        {
            return Info(message, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Info(string message, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Info(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Info(string message, string caption)
        {
            return Info(message, caption, MessageBoxButton.OK);
        }

        public static MessageBoxResult Info(string message, string caption, MessageBoxButton button)
        {
            return Info(message, caption, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Info(string message, string caption, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxImage.Information, defaultResult);
        }
        #endregion

        #region Question
        public static MessageBoxResult Question(string message)
        {
            return Question(message, MessageBoxButton.YesNo);
        }

        public static MessageBoxResult Question(string message, MessageBoxButton button)
        {
            return Question(message, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Question(string message, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Question(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Question(string message, string caption)
        {
            return Question(message, caption, MessageBoxButton.YesNo);
        }

        public static MessageBoxResult Question(string message, string caption, MessageBoxButton button)
        {
            return Question(message, caption, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Question(string message, string caption, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxImage.Question, defaultResult);
        }
        #endregion

        #region Warning
        public static MessageBoxResult Warning(string message)
        {
            return Warning(message, MessageBoxButton.OK);
        }

        public static MessageBoxResult Warning(string message, MessageBoxButton button)
        {
            return Warning(message, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Warning(string message, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Warning(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Warning(string message, string caption)
        {
            return Warning(message, caption, MessageBoxButton.OK);
        }

        public static MessageBoxResult Warning(string message, string caption, MessageBoxButton button)
        {
            return Warning(message, caption, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Warning(string message, string caption, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxImage.Warning, defaultResult);
        }
        #endregion

        #region Error
        public static MessageBoxResult Error(string message)
        {
            return Error(message, MessageBoxButton.OK);
        }

        public static MessageBoxResult Error(string message, MessageBoxButton button)
        {
            return Error(message, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Error(string message, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Error(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Error(string message, string caption)
        {
            return Error(message, caption, MessageBoxButton.OK);
        }

        public static MessageBoxResult Error(string message, string caption, MessageBoxButton button)
        {
            return Error(message, caption, button, MessageBoxResult.None);
        }

        public static MessageBoxResult Error(string message, string caption, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxImage.Error, defaultResult);
        }
        #endregion

        private static MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            var options = MessageBoxOptions.None;

            if (LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft)
            {
                options |= MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading;
            }

            return MessageBox.Show(message, caption, button, icon, defaultResult, options);
        }
    }
}
