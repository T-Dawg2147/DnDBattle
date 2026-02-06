using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.Effects
{
    public class SoundEffectsService
    {
        private Dictionary<SoundEffect, MediaPlayer> _sounds = new Dictionary<SoundEffect, MediaPlayer>();
        private string _soundsDirectory;

        public bool IsEnabled { get; set; } = true;
        public double Volume { get; set; } = 0.5;

        public SoundEffectsService()
        {
            _soundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            Directory.CreateDirectory(_soundsDirectory);

            LoadSounds();
        }

        private void LoadSounds()
        {
            var soundFiles = new Dictionary<SoundEffect, string>
            {
                { SoundEffect.DiceRoll, "dice_roll.wav" },
                { SoundEffect.CriticalHit, "critical_hit.wav" },
                { SoundEffect.CriticalMiss, "critical_miss.wav" },
                { SoundEffect.Hit, "hit.wav" },
                { SoundEffect.Miss, "miss.wav" },
                { SoundEffect.Death, "death.wav" },
                { SoundEffect.Heal, "heal.wav" },
                { SoundEffect.SpellCast, "spell_cast.wav" },
                { SoundEffect.TurnStart, "turn_start.wav" },
                { SoundEffect.CombatStart, "combat_start.wav" },
                { SoundEffect.CombatEnd, "combat_end.wav" },
                { SoundEffect.TimerWarning, "timer_warning.wav" },
                { SoundEffect.TimerExpired, "timer_expired.wav" },
                { SoundEffect.LegendaryAction, "legendary_action.wav" },
                { SoundEffect.LairAction, "lair_action.wav" }
            };

            foreach (var kvp in soundFiles)
            {
                string filePath = Path.Combine(_soundsDirectory, kvp.Value);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var player = new MediaPlayer();
                        player.Open(new Uri(filePath));
                        player.Volume = Volume;
                        _sounds[kvp.Key] = player;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading sound {kvp.Value}: {ex.Message}");
                    }
                }
            }
        }

        public void Play(SoundEffect effect)
        {
            if (!IsEnabled) return;

            if (_sounds.TryGetValue(effect, out var player))
            {
                try
                {
                    player.Volume = Volume;
                    player.Position = TimeSpan.Zero;
                    player.Play();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error playing sound: {ex.Message}");
                }
            }
            else
            {
                PlaySystemSound(effect);
            }
        }

        private void PlaySystemSound(SoundEffect effect)
        {
            try
            {
                switch (effect)
                {
                    case SoundEffect.CriticalHit:
                    case SoundEffect.CombatStart:
                        SystemSounds.Exclamation.Play();
                        break;
                    case SoundEffect.CriticalMiss:
                    case SoundEffect.Death:
                        SystemSounds.Hand.Play();
                        break;
                    case SoundEffect.TimerWarning:
                    case SoundEffect.TimerExpired:
                        SystemSounds.Beep.Play();
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        public void SetVolumn(double volume)
        {
            Volume = Math.Clamp(volume, 0.0, 1.0);
            foreach (var player in _sounds.Values)
                player.Volume = Volume;
        }

        public void StopAll()
        {
            foreach (var player in _sounds.Values)
                player.Stop();
        }
    }

    public enum SoundEffect
    {
        DiceRoll,
        CriticalHit,
        CriticalMiss,
        Hit,
        Miss,
        Death,
        Heal,
        SpellCast,
        TurnStart,
        CombatStart,
        CombatEnd,
        TimerWarning,
        TimerExpired,
        LegendaryAction,
        LairAction
    }
}
