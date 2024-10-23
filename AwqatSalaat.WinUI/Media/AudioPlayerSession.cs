using System;

namespace AwqatSalaat.WinUI.Media
{
    internal class AudioPlayerSession
    {
        private bool _endeding;

        public string File { get; init; }
        public string Tag { get; init; }
        public bool Loop { get; init; }

        public event Action Ended;

        public void End()
        {
            if (!_endeding)
            {
                _endeding = true;
                Ended?.Invoke();
                _endeding = false;
            }
        }
    }
}
