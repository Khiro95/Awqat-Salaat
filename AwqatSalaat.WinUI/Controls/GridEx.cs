using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    internal class GridEx : Grid
    {
        public void SetCursor(InputSystemCursorShape cursorShape) => ProtectedCursor = InputSystemCursor.Create(cursorShape);

        public void ResetCursor() => ProtectedCursor = null;
    }
}
