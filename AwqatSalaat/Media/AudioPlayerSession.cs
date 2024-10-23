using System;

namespace AwqatSalaat.Media
{
    internal class AudioPlayerSession
    {
        private bool _endeding;

        public string File { get; }
        public string Tag { get; }
        public bool Loop { get; set; }

        public event Action Ended;

        public AudioPlayerSession(string file, string tag, bool loop = false)
        {
            File = file;
            Tag = tag;
            Loop = loop;
        }

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
