using System;

namespace AwqatSalaat.WinUI
{
    internal class WidgetNotInjectedException : Exception
    {
        public WidgetNotInjectedException(string message) : base(message) { }
    }
}
