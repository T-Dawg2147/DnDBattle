using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public class DiceHistoryService
    {
        public ObservableCollection<DiceRollRecord> History { get; } = new ObservableCollection<DiceRollRecord>();

        public int MaxHistorySize { get; set; } = 500;

        public event Action<DiceRollRecord> RollRecorded;

        public void RecordRoll(DiceRollRecord record)
        {
            History.Insert(0, record);

            // Trim history if too large
            while (History.Count > MaxHistorySize)
            {
                History.RemoveAt(History.Count - 1);
            }

            RollRecorded?.Invoke(record);
        }

        public void RecordRoll(string roller, string expression, int result, DiceRollType rollType, string context = null)
        {
            var record = new DiceRollRecord
            {
                Timestamp = DateTime.Now,
                RollerName = roller,
                Expression = expression,
                Result = result,
                RollType = rollType,
                Context = context
            };

            RecordRoll(record);
        }

        public IEnumerable<DiceRollRecord> GetByRoller(string rollerName)
        {
            return History.Where(r => r.RollerName.Equals(rollerName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<DiceRollRecord> GetByType(DiceRollType rollType)
        {
            return History.Where(r => r.RollType == rollType);
        }

        public IEnumerable<DiceRollRecord> GetRecent(int count)
        {
            return History.Take(count);
        }

        public DiceRollStatistics GetStatistics(string rollerName = null)
        {
            var rolls = string.IsNullOrEmpty(rollerName)
                ? History
                : History.Where(r => r.RollerName.Equals(rollerName, StringComparison.OrdinalIgnoreCase));

            var d20Rolls = rolls.Where(r => r.Expression.Contains("d20")).ToList();

            return new DiceRollStatistics
            {
                TotalRolls = rolls.Count(),
                D20Rolls = d20Rolls.Count,
                Natural20s = d20Rolls.Count(r => r.NaturalRoll == 20),
                Natural1s = d20Rolls.Count(r => r.NaturalRoll == 1),
                AverageD20 = d20Rolls.Any() ? d20Rolls.Average(r => r.NaturalRoll ?? r.Result) : 0.0,
                HighestRoll = rolls.Any() ? rolls.Max(r => r.Result) : 0,
                LowestRoll = rolls.Any() ? rolls.Min(r => r.Result) : 0
            };
        }

        public void Clear()
        {
            History.Clear();
        }
    }

    public class DiceRollRecord
    {
        public DateTime Timestamp { get; set; }
        public string RollerName { get; set; }
        public string Expression { get; set; }
        public int Result { get; set; }
        public int? NaturalRoll { get; set; }
        public DiceRollType RollType { get; set; }
        public string Context{ get; set; }
        public bool IsCritical => NaturalRoll == 20;
        public bool IsCriticalFail => NaturalRoll == 1;

        public string DisplayText => $"{RollerName}: {Expression} = {Result}";
        public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    }

    public class DiceRollStatistics
    {
        public int TotalRolls { get; set; }
        public int D20Rolls { get; set; }
        public int Natural20s { get; set; }
        public int Natural1s { get; set; }
        public double AverageD20 { get; set; }
        public int HighestRoll { get; set; }
        public int LowestRoll { get; set; }

        public double CritRate => D20Rolls > 0 ? (double)Natural20s / D20Rolls * 100 : 0;
        public double CritFailRate => D20Rolls > 0 ? (double)Natural1s / D20Rolls * 100 : 0;
    }

    public enum DiceRollType
    {
        Attack, Damage, SavingThrow,
        AbilityCheck, Initiative, Healing,
        DeathSave, Concentration, Other
    }
}
