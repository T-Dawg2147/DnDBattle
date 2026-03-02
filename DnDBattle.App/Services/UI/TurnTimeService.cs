using System;
using System.Windows.Threading;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.UI
{
    public class TurnTimerService
    {
        private DispatcherTimer _timer;
        private DateTime _turnStartTime;
        private TimeSpan _timeLimit;
        private bool _isRunning;

        public event Action<TimeSpan> TimerTick;
        public event Action TimerExpired;
        public event Action<TimeSpan> TurnEnded;

        public bool IsEnabled { get; set; } = false;
        public TimeSpan TimeLimit
        {
            get => _timeLimit;
            set => _timeLimit = value;
        }

        public TimeSpan TimeRemaining => _isRunning ? _timeLimit - Elapsed : _timeLimit;
        public TimeSpan Elapsed => _isRunning ? DateTime.Now - _turnStartTime : TimeSpan.Zero;
        public bool IsExpired => _isRunning && Elapsed >= _timeLimit;
        public bool IsRunning => _isRunning;

        public TurnTimerService()
        {
            _timeLimit = TimeSpan.FromMinutes(2); // Default 2 minutes per turn
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            var remaining = TimeRemaining;
            TimerTick?.Invoke(remaining);

            if (remaining <= TimeSpan.Zero)
            {
                TimerExpired?.Invoke();
                Stop();
            }
        }

        public void Start()
        {
            if (!IsEnabled) return;

            _turnStartTime = DateTime.Now;
            _isRunning = true;
            _timer.Start();
        }

        public void Stop()
        {
            var elapsed = Elapsed;
            _timer.Stop();
            _isRunning = false;
            TurnEnded?.Invoke(elapsed);
        }

        public void Reset()
        {
            _turnStartTime = DateTime.Now;
            _isRunning = true;
        }

        public void Pause()
        {
            _timer.Stop();
        }

        public void Resume()
        {
            _timer.Start();
        }

        public void SetTimeLimit(int seconds)
        {
            _timeLimit = TimeSpan.FromSeconds(seconds);
        }

        public void SetTimeLimit(TimeSpan time)
        {
            _timeLimit = time;
        }
    }
}