using Serilog;
using System;
using System.IO;
using System.Windows.Media;

namespace AwqatSalaat.Media
{
    internal static class AudioPlayer
    {
        private static readonly MediaPlayer s_mediaPlayer = new MediaPlayer() { Volume = 1 };
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
                s_mediaPlayer.Open(new Uri(session.File));
                s_mediaPlayer.Position = TimeSpan.Zero;
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

        private static void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if ((s_currentSession?.Loop) == true)
            {
                s_mediaPlayer.Position = TimeSpan.Zero;
            }
            else
            {
                Stop();
            }
        }

        private static void Stop()
        {
            if (s_currentSession != null)
            {
                Log.Information($"Stopping audio (tag: {s_currentSession.Tag})");
                s_currentSession.Ended -= Session_Ended;
                s_currentSession.End();
                s_currentSession = null;
                s_mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                s_mediaPlayer.Close();

                Stopped?.Invoke();
            }
        }
    }
}
