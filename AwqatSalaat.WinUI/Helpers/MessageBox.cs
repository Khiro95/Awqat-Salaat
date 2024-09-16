using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.Properties;
using System;

namespace AwqatSalaat.WinUI.Helpers
{
    internal static class MessageBox
    {
        #region Info
        public static MessageBoxResult Info(string message)
        {
            return Info(message, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Info(string message, MessageBoxButtons button)
        {
            return Info(message, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Info(string message, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Info(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Info(string message, string caption)
        {
            return Info(message, caption, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Info(string message, string caption, MessageBoxButtons button)
        {
            return Info(message, caption, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Info(string message, string caption, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxIcon.MB_ICONINFORMATION, defaultResult);
        }
        #endregion

        #region Question
        public static MessageBoxResult Question(string message)
        {
            return Question(message, MessageBoxButtons.MB_YESNO);
        }

        public static MessageBoxResult Question(string message, MessageBoxButtons button)
        {
            return Question(message, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Question(string message, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Question(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Question(string message, string caption)
        {
            return Question(message, caption, MessageBoxButtons.MB_YESNO);
        }

        public static MessageBoxResult Question(string message, string caption, MessageBoxButtons button)
        {
            return Question(message, caption, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Question(string message, string caption, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxIcon.MB_ICONQUESTION, defaultResult);
        }
        #endregion

        #region Warning
        public static MessageBoxResult Warning(string message)
        {
            return Warning(message, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Warning(string message, MessageBoxButtons button)
        {
            return Warning(message, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Warning(string message, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Warning(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Warning(string message, string caption)
        {
            return Warning(message, caption, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Warning(string message, string caption, MessageBoxButtons button)
        {
            return Warning(message, caption, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Warning(string message, string caption, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxIcon.MB_ICONWARNING, defaultResult);
        }
        #endregion

        #region Error
        public static MessageBoxResult Error(string message)
        {
            return Error(message, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Error(string message, MessageBoxButtons button)
        {
            return Error(message, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Error(string message, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Error(message, Resources.Data_AppName, button, defaultResult);
        }

        public static MessageBoxResult Error(string message, string caption)
        {
            return Error(message, caption, MessageBoxButtons.MB_OK);
        }

        public static MessageBoxResult Error(string message, string caption, MessageBoxButtons button)
        {
            return Error(message, caption, button, MessageBoxResult.NONE);
        }

        public static MessageBoxResult Error(string message, string caption, MessageBoxButtons button, MessageBoxResult defaultResult)
        {
            return Show(message, caption, button, MessageBoxIcon.MB_ICONERROR, defaultResult);
        }
        #endregion

        private static MessageBoxResult Show(string message, string caption, MessageBoxButtons button, MessageBoxIcon icon, MessageBoxResult defaultResult)
        {
            return Show(IntPtr.Zero, message, caption, button, icon, defaultResult);
        }

        private static MessageBoxResult Show(IntPtr HWND, string message, string caption, MessageBoxButtons button, MessageBoxIcon icon, MessageBoxResult defaultResult)
        {
            var options = MessageBoxOptions.NONE;

            if (LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft)
            {
                options |= MessageBoxOptions.MB_RIGHT | MessageBoxOptions.MB_RTLREADING;
            }

            uint type = (uint)button | (uint)icon | DefaultResultToButtonNumber(defaultResult, button) | (uint)options;

            return (MessageBoxResult)User32.MessageBox(HWND, message, caption, type);
        }

        private static uint DefaultResultToButtonNumber(MessageBoxResult result, MessageBoxButtons button)
        {
            if (result == MessageBoxResult.NONE)
            {
                return 0;
            }

            switch (button)
            {
                case MessageBoxButtons.MB_OK:
                    return 0;
                case MessageBoxButtons.MB_OKCANCEL:
                    if (result == MessageBoxResult.IDCANCEL)
                    {
                        return 256;
                    }

                    return 0;
                case MessageBoxButtons.MB_YESNO:
                    if (result == MessageBoxResult.IDNO)
                    {
                        return 256;
                    }

                    return 0;
                case MessageBoxButtons.MB_YESNOCANCEL:
                    return result switch
                    {
                        MessageBoxResult.IDNO => 256,
                        MessageBoxResult.IDCANCEL => 512,
                        _ => 0,
                    };
                default:
                    return 0;
            }
        }
    }
}
