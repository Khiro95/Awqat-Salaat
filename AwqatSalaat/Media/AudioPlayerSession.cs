using System;

namespace AwqatSalaat.Media
{
    internal class AudioPlayerSession
    {
        private bool _ending;

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
            if (!_ending)
            {
                _ending = true;
                Ended?.Invoke();
                _ending = false;
            }
        }
    }
}
