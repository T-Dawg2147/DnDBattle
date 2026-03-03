using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Services.TileService
{
    public class HazardTrackingService
    {
        private readonly Dictionary<Guid, List<HazardMetadata>> _tokenHazards = new Dictionary<Guid, List<HazardMetadata>>();

        public event Action<string> LogMessage;
        public event Action<Token, HazardMetadata, int> HazardDamageApplied;

        public void UpdateTokenPosition(Token token, TileMap tileMap)
        {
            if (tileMap == null || token == null) return;

            var tile = tileMap.GetTilesAt(token.GridX, token.GridY).FirstOrDefault();
            var hazards = tile?.GetMetadata(TileMetadataType.Hazard).OfType<HazardMetadata>().ToList()
                ?? new List<HazardMetadata>();

            if (hazards.Count > 0)
            {
                _tokenHazards[token.Id] = hazards;
            }
            else
            {
                _tokenHazards.Remove(token.Id);
            }
        }

        public void ApplyStartOfTurnDamage(Token token, MetadataInteractionService metadataService)
        {
            if (!_tokenHazards.ContainsKey(token.Id)) return;

            var hazards = _tokenHazards[token.Id];
            foreach (var hazard in hazards.Where(h => h.DamagesEachTurn && h.DamageTrigger == HazardTrigger.StartOfTurn))
                ApplyHazardDamage(token, hazard, metadataService);
        }

        public void ApplyEndOfTurnDamage(Token token, MetadataInteractionService metadataService)
        {
            if (!_tokenHazards.ContainsKey(token.Id)) return;

            var hazards = _tokenHazards[token.Id];
            foreach (var hazard in hazards.Where(h => h.DamagesEachTurn && h.DamageTrigger == HazardTrigger.EndOfTurn))
                ApplyHazardDamage(token, hazard, metadataService);
        }

        private void ApplyHazardDamage(Token token, HazardMetadata hazard, MetadataInteractionService metadataService)
        {
            if (!hazard.DamagesEachTurn) return;

            LogMessage?.Invoke($"☠️ {token.Name} takes damage from {hazard.Name}");

            var damageRoll = Utils.DiceRoller.RollExpression(hazard.PerTurnDamage ?? hazard.DamageDice);
            int damage = damageRoll.Total;

            var (effectiveDamage, desc) = token.TakeDamage(damage, hazard.DamageType);

            LogMessage?.Invoke($"{hazard.DamageType.GetIcon()} {token.Name} takes {effectiveDamage} {hazard.DamageType.GetDisplayName()} damage! ({damageRoll.Expression} = {damageRoll.Total})");

            HazardDamageApplied?.Invoke(token, hazard, effectiveDamage);
        }

        public Dictionary<Guid, List<HazardMetadata>> GetActiveHazard()
        {
            return new Dictionary<Guid, List<HazardMetadata>>(_tokenHazards);
        }

        public void Clear()
        {
            _tokenHazards.Clear();
        }
    }
}
