using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDBattle.Models
{
    public class SpellSlots : ObservableObject
    {
        private int[] _maxSlots = new int[10]; // Index 0 = cantrips (unused), 1-9 = spell levels
        private int[] _currentSlots = new int[10];

        public int GetMaxSlots(int level) => level >= 1 && level <= 9 ? _maxSlots[level] : 0;
        public int GetCurrentSlots(int level) => level >= 1 && level <= 9 ? _currentSlots[level] : 0;

        public void SetMaxSlots(int level, int count)
        {
            if (level >= 1 && level <= 9)
            {
                _maxSlots[level] = count;
                _currentSlots[level] = Math.Min(_currentSlots[level], count);
                OnPropertyChanged(nameof(HasSpellSlots));
                OnPropertyChanged($"Level{level}Display");
            }
        }

        public void SetCurrentSlots(int level, int count)
        {
            if (level >= 1 && level <= 9)
            {
                _currentSlots[level] = Math.Clamp(count, 0, _maxSlots[level]);
                OnPropertyChanged($"Level{level}Display");
            }
        }

        public bool UseSlot(int level)
        {
            if (level >= 1 && level <= 9 && _currentSlots[level] > 0)
            {
                _currentSlots[level]--;
                OnPropertyChanged($"Level{level}Display");
                return true;
            }
            return false;
        }

        public void RestoreSlot(int level)
        {
            if (level >= 1 && level <= 9 && _currentSlots[level] < _maxSlots[level])
            {
                _currentSlots[level]++;
                OnPropertyChanged($"Level{level}Display");
            }
        }

        public void RestoreAllSlots()
        {
            for (int i = 1; i <= 9; i++)
            {
                _currentSlots[i] = _maxSlots[i];
                OnPropertyChanged($"Level{i}Display");
            }
        }

        public void ShortRest()
        {
            // Warlocks restore on short rest - could be extended
            // For now, just trigger event
        }

        public void LongRest()
        {
            RestoreAllSlots();
        }

        public bool HasSpellSlots => _maxSlots.Skip(1).Any(s => s > 0);

        public string GetDisplayForLevel(int level)
        {
            if (level < 1 || level > 9 || _maxSlots[level] == 0)
                return null;
            return $"{_currentSlots[level]}/{_maxSlots[level]}";
        }

        // Display properties for binding
        public string Level1Display => GetDisplayForLevel(1);
        public string Level2Display => GetDisplayForLevel(2);
        public string Level3Display => GetDisplayForLevel(3);
        public string Level4Display => GetDisplayForLevel(4);
        public string Level5Display => GetDisplayForLevel(5);
        public string Level6Display => GetDisplayForLevel(6);
        public string Level7Display => GetDisplayForLevel(7);
        public string Level8Display => GetDisplayForLevel(8);
        public string Level9Display => GetDisplayForLevel(9);

        /// <summary>
        /// Gets spell slots for a class at a given level (5e standard progression)
        /// </summary>
        public static SpellSlots GetForCasterLevel(int casterLevel, CasterType casterType = CasterType.Full)
        {
            var slots = new SpellSlots();

            // Full caster progression (Wizard, Cleric, Druid, Bard, Sorcerer)
            var fullCasterSlots = new Dictionary<int, int[]>
            {
                { 1, new[] { 2, 0, 0, 0, 0, 0, 0, 0, 0 } },
                { 2, new[] { 3, 0, 0, 0, 0, 0, 0, 0, 0 } },
                { 3, new[] { 4, 2, 0, 0, 0, 0, 0, 0, 0 } },
                { 4, new[] { 4, 3, 0, 0, 0, 0, 0, 0, 0 } },
                { 5, new[] { 4, 3, 2, 0, 0, 0, 0, 0, 0 } },
                { 6, new[] { 4, 3, 3, 0, 0, 0, 0, 0, 0 } },
                { 7, new[] { 4, 3, 3, 1, 0, 0, 0, 0, 0 } },
                { 8, new[] { 4, 3, 3, 2, 0, 0, 0, 0, 0 } },
                { 9, new[] { 4, 3, 3, 3, 1, 0, 0, 0, 0 } },
                { 10, new[] { 4, 3, 3, 3, 2, 0, 0, 0, 0 } },
                { 11, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 } },
                { 12, new[] { 4, 3, 3, 3, 2, 1, 0, 0, 0 } },
                { 13, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 } },
                { 14, new[] { 4, 3, 3, 3, 2, 1, 1, 0, 0 } },
                { 15, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 } },
                { 16, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 0 } },
                { 17, new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 } },
                { 18, new[] { 4, 3, 3, 3, 3, 1, 1, 1, 1 } },
                { 19, new[] { 4, 3, 3, 3, 3, 2, 1, 1, 1 } },
                { 20, new[] { 4, 3, 3, 3, 3, 2, 2, 1, 1 } }
            };

            casterLevel = Math.Clamp(casterLevel, 1, 20);
            int effectiveLevel = casterLevel;

            if (casterType == CasterType.Half)
                effectiveLevel = Math.Max(1, casterLevel / 2);
            else if (casterType == CasterType.Third)
                effectiveLevel = Math.Max(1, casterLevel / 3);

            if (fullCasterSlots.TryGetValue(effectiveLevel, out var slotArray))
            {
                for (int i = 0; i < 9; i++)
                {
                    slots.SetMaxSlots(i + 1, slotArray[i]);
                    slots.SetCurrentSlots(i + 1, slotArray[i]);
                }
            }

            return slots;
        }
    }

    public enum CasterType
    {
        Full,   // Wizard, Cleric, Druid, Bard, Sorcerer
        Half,   // Paladin, Ranger
        Third,  // Eldritch Knight, Arcane Trickster
        Warlock // Pact magic (different system)
    }
}