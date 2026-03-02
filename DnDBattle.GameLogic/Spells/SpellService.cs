using DnDBattle.Core.Events;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace DnDBattle.GameLogic.Spells;

public sealed class SpellService : ISpellService
{
    private readonly IDiceService _dice;
    private readonly IMessenger _messenger;
    private readonly Dictionary<Guid, int[]> _spellSlots = new();
    private readonly List<SpellDefinition> _spellLibrary = new();

    public SpellService(IDiceService dice, IMessenger messenger)
    {
        _dice = dice;
        _messenger = messenger;
        PopulateDefaultLibrary();
    }

    public bool TryCastSpell(Combatant caster, SpellDefinition spell, IEnumerable<Combatant> targets)
    {
        if (spell.Level > 0 && !HasSpellSlot(caster, spell.Level)) return false;
        if (spell.Level > 0) UseSpellSlot(caster, spell.Level);

        foreach (var target in targets)
        {
            if (spell.SaveAbility.HasValue)
            {
                int roll = _dice.Roll(20) + target.GetAbilityModifier(spell.SaveAbility.Value);
                bool saved = roll >= spell.SaveDC;
                if (spell.DamageType == Core.Enums.DamageType.None)
                {
                    // Healing spell — restore HP on save or full on failure
                    int healing = _dice.ParseAndRoll(spell.DamageDice);
                    if (saved) healing = 0; // e.g. Hold Person — no HP component
                    target.CurrentHitPoints = Math.Min(target.MaxHitPoints, target.CurrentHitPoints + healing);
                }
                else
                {
                    int damage = _dice.ParseAndRoll(spell.DamageDice);
                    if (saved) damage /= 2;
                    target.CurrentHitPoints = Math.Max(0, target.CurrentHitPoints - damage);
                }
            }
            else if (spell.DamageType == Core.Enums.DamageType.None)
            {
                // Healing spell without save (e.g. Cure Wounds)
                int healing = _dice.ParseAndRoll(spell.DamageDice);
                target.CurrentHitPoints = Math.Min(target.MaxHitPoints, target.CurrentHitPoints + healing);
            }
            else
            {
                int damage = _dice.ParseAndRoll(spell.DamageDice);
                target.CurrentHitPoints = Math.Max(0, target.CurrentHitPoints - damage);
            }
        }

        _messenger.Send(new SpellCastMessage(caster.Id, spell.Id, spell.Name));
        return true;
    }

    public bool HasSpellSlot(Combatant caster, int level)
    {
        if (level is < 1 or > 9) return false;
        return _spellSlots.TryGetValue(caster.Id, out var slots) && slots[level - 1] > 0;
    }

    public void UseSpellSlot(Combatant caster, int level)
    {
        if (_spellSlots.TryGetValue(caster.Id, out var slots) && level >= 1 && level <= 9)
            slots[level - 1] = Math.Max(0, slots[level - 1] - 1);
    }

    public void RestoreSpellSlots(Combatant caster)
    {
        _spellSlots[caster.Id] = new[] { 4, 3, 3, 3, 2, 1, 1, 1, 1 };
    }

    public IReadOnlyList<SpellDefinition> GetAvailableSpells(Combatant caster) =>
        _spellLibrary;

    private void PopulateDefaultLibrary()
    {
        _spellLibrary.AddRange(new[]
        {
            new SpellDefinition(Guid.NewGuid(), "Magic Missile",
                Core.Enums.SpellSchool.Evocation, 1, "1 action", "120 feet", "V, S", "Instantaneous",
                false, null, null, "3d4+3", Core.Enums.DamageType.Force, null, 0,
                "Three darts of magical force strike targets."),
            new SpellDefinition(Guid.NewGuid(), "Fireball",
                Core.Enums.SpellSchool.Evocation, 3, "1 action", "150 feet", "V, S, M",
                "Instantaneous", false, Core.Enums.AreaEffectShape.Sphere, 20, "8d6",
                Core.Enums.DamageType.Fire, Core.Enums.Ability.Dexterity, 14,
                "A bright streak flashes to a point and blossoms into a fiery explosion."),
            new SpellDefinition(Guid.NewGuid(), "Cure Wounds",
                Core.Enums.SpellSchool.Evocation, 1, "1 action", "Touch", "V, S",
                "Instantaneous", false, null, null, "1d8+3",
                Core.Enums.DamageType.None, null, 0,
                "A creature you touch regains hit points."),
            new SpellDefinition(Guid.NewGuid(), "Hold Person",
                Core.Enums.SpellSchool.Enchantment, 2, "1 action", "60 feet", "V, S, M",
                "1 minute", true, null, null, "0",
                Core.Enums.DamageType.None, Core.Enums.Ability.Wisdom, 14,
                "Choose a humanoid that you can see. The target must succeed or be paralyzed."),
        });
    }
}
