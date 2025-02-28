using Serilog;
using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace AwqatSalaat.WinUI.Media
{
    internal static class AudioPlayer
    {
        private static readonly MediaPlayer s_mediaPlayer = new MediaPlayer();
        private static AudioPlayerSession s_currentSession;

        public static AudioPlayerSession CurrentSession => s_currentSession;

        public static event Action Started;
        public static event Action Stopped;

        public static bool Play(AudioPlayerSession session)
        {
            Stop();

            Log.Information($"Requested to play an audio (tag: {session.Tag})");
            Log.Debug("Audio session: {@session}", session);

            if (File.Exists(session.File))
            {
                s_mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                s_mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
                s_mediaPlayer.Position = TimeSpan.Zero;
                s_mediaPlayer.IsLoopingEnabled = session.Loop;
                s_mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(session.File));
                s_mediaPlayer.Play();
                s_currentSession = session;
                s_currentSession.Ended += Session_Ended;

                Started?.Invoke();

                return true;
            }

            Log.Information("Audio file not found");
            return false;
        }

        private static void Session_Ended() => Stop();

        private static void MediaPlayer_MediaEnded(MediaPlayer sender, object args) => Stop();

        private static void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Log.Warning($"Failed to play audio. Error=0x{args.ExtendedErrorCode.HResult:X2}");
            Stop();
        }

        private static void Stop()
        {
            if (s_currentSession is not null)
            {
                Log.Information($"Stopping audio (tag: {s_currentSession.Tag})");
                s_currentSession.Ended -= Session_Ended;
                s_currentSession.End();
                s_currentSession = null;
                s_mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                s_mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                (s_mediaPlayer.Source as IDisposable)?.Dispose();
                s_mediaPlayer.Source = null;

                Stopped?.Invoke();
            }
        }
    }
}
