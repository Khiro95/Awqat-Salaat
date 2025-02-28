using AwqatSalaat.WinUI.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;

namespace AwqatSalaat.WinUI.Controls
{
    internal class SoundPreviewButton : Control
    {
        private Button _button;
        private bool _isPlaying;
        private AudioPlayerSession _session;

        public static readonly DependencyProperty IdleTooltipProperty = DependencyProperty.Register(
            nameof(IdleToolTip),
            typeof(string),
            typeof(SoundPreviewButton),
            new PropertyMetadata(null, OnToolTipChanged));

        public static readonly DependencyProperty PlayingTooltipProperty = DependencyProperty.Register(
            nameof(PlayingToolTip),
            typeof(string),
            typeof(SoundPreviewButton),
            new PropertyMetadata(null, OnToolTipChanged));

        private static void OnToolTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var preview = (SoundPreviewButton)d;
            preview.UpdateToolTip();
        }

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

            preview.IsEnabled = !string.IsNullOrEmpty((string)e.NewValue);
        }

        public string File { get => (string)GetValue(FileProperty); set => SetValue(FileProperty, value); }
        public string IdleToolTip { get => (string)GetValue(IdleTooltipProperty); set => SetValue(IdleTooltipProperty, value); }
        public string PlayingToolTip { get => (string)GetValue(PlayingTooltipProperty); set => SetValue(PlayingTooltipProperty, value); }
        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                _isPlaying = value;
                VisualStateManager.GoToState(this, value ? "Playing" : "Idle", false);
                UpdateToolTip();
            }
        }

        public SoundPreviewButton()
        {
            DefaultStyleKey = typeof(SoundPreviewButton);

            Loaded += SoundPreviewButton_Loaded;
            Unloaded += SoundPreviewButton_Unloaded;
        }

        private void SoundPreviewButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (_button is not null)
            {
                _button.Click -= SoundPreviewButton_Click;
                _button.Click += SoundPreviewButton_Click;
            }
        }

        private void SoundPreviewButton_Unloaded(object sender, RoutedEventArgs e)
        {
            Stop();
            _button.Click -= SoundPreviewButton_Click;
        }

        private void SoundPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
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
            _session = new AudioPlayerSession
            {
                File = File,
                Tag = "SoundPreviewButton"
            };
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
            DispatcherQueue.TryEnqueue(() => IsPlaying = false);
        }

        private void UpdateToolTip() => SetValue(ToolTipService.ToolTipProperty, IsPlaying ? PlayingToolTip : IdleToolTip);

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _button = GetTemplateChild("PART_Button") as Button;
            _button.Click += SoundPreviewButton_Click;
        }
    }
}
