using System;

namespace AwqatSalaat.WinUI.Media
{
    internal class AudioPlayerSession
    {
        private bool _ending;

        public string File { get; init; }
        public string Tag { get; init; }
        public bool Loop { get; init; }

        public event Action Ended;

        public void End()
        {
            if (!_ending)
            {
                _ending = true;
                Ended?.Invoke();
                _ending = false;
            }
        }
    }
}
