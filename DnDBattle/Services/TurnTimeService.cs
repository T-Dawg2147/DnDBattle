using System;
using System.Windows.Threading;

namespace DnDBattle.Services
{
    public class TurnTimerService
    {
        private DispatcherTimer _timer;
        private DateTime _turnStartTime;
        private TimeSpan _timeLimit;
        private bool _isRunning;
        
        // Cached values to avoid recalculating and reduce UI updates
        private TimeSpan _cachedTimeRemaining;
        private int _lastDisplayedSeconds = -1;

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
            // Increased interval from 100ms to 250ms - still smooth updates but fewer ticks
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            _cachedTimeRemaining = TimeRemaining;
            
            // Only fire TimerTick if the displayed seconds actually changed
            // This reduces UI update frequency when remaining time is > 1 minute
            int currentSeconds = (int)_cachedTimeRemaining.TotalSeconds;
            if (currentSeconds != _lastDisplayedSeconds)
            {
                _lastDisplayedSeconds = currentSeconds;
                TimerTick?.Invoke(_cachedTimeRemaining);
            }

            if (_cachedTimeRemaining <= TimeSpan.Zero)
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
            _lastDisplayedSeconds = -1; // Reset to force first update
            _timer.Start();
        }

        public void Stop()
        {
            var elapsed = Elapsed;
            _timer.Stop();
            _isRunning = false;
            _lastDisplayedSeconds = -1;
            TurnEnded?.Invoke(elapsed);
        }

        public void Reset()
        {
            _turnStartTime = DateTime.Now;
            _isRunning = true;
            _lastDisplayedSeconds = -1; // Reset to force update
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