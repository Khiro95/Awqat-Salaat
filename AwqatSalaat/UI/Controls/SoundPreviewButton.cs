using AwqatSalaat.Media;
using System.Windows;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Controls
{
    internal class SoundPreviewButton : Button
    {
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register(
            nameof(File),
            typeof(string),
            typeof(SoundPreviewButton),
            new PropertyMetadata(null, OnFileChanged));

        private static void OnFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var preview = (SoundPreviewButton)d;

            if (preview.IsPlaying)
            {
                preview.Stop();
            }
        }

        public static readonly DependencyPropertyKey IsPlayingPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsPlaying),
            typeof(bool),
            typeof(SoundPreviewButton),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsPlayingProperty = IsPlayingPropertyKey.DependencyProperty;

        public string File { get => (string)GetValue(FileProperty); set => SetValue(FileProperty, value); }
        public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); private set => SetValue(IsPlayingPropertyKey, value); }

        private AudioPlayerSession _session;

        public SoundPreviewButton()
        {
            Unloaded += (_, __) => Stop();
        }

        protected override void OnClick()
        {
            base.OnClick();

            if (IsPlaying)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        private void Play()
        {
            _session = new AudioPlayerSession(File, tag: "SoundPreviewButton");
            _session.Ended += AudioSession_Ended;
            bool started = AudioPlayer.Play(_session);
            IsPlaying = started;

            if (!started)
            {
                _session.Ended -= AudioSession_Ended;
                _session = null;
            }
        }

        private void Stop() => _session?.End();

        private void AudioSession_Ended()
        {
            _session.Ended -= AudioSession_Ended;
            _session = null;
            IsPlaying = false;
        }
    }
}
