using System;
using System.Collections.Generic;
using System.Media;
using System.Text;

namespace Displacement
{
    public class AlertService
    {
        private SoundPlayer? _player;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public void StartAlarm()
        {
            if (_isPlaying) return;

            string path = Path.Combine(
                Application.StartupPath,
                "alarm.wav");

            _player = new SoundPlayer(path);
            _player.PlayLooping();

            _isPlaying = true;
        }

        public void StopAlarm()
        {
            if (!_isPlaying) return;

            _player?.Stop();
            _isPlaying = false;
        }
    }
}
