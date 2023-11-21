using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("77d75317-01c7-4fad-b1b1-eb08832b649e")]
    internal class EldritchBlastCalculateSpellLevel : ContextCalculateAbilityParams
    {
        public EldritchBlastCalculateSpellLevel() : this(1) { }

        public EldritchBlastCalculateSpellLevel(int equivalentSpellLevel)
        {
            SpellLevel = BaseEquivalentSpellLevel = equivalentSpellLevel;
            StatType = StatType.Charisma;
            ReplaceSpellLevel = true;
        }

        public int BaseEquivalentSpellLevel;

        public override AbilityParams Calculate(MechanicsContext context)
        {
            RecalculateSpellLevel(context);

            return base.Calculate(context);
        }

        public void RecalculateSpellLevel(MechanicsContext context)
        {
            if (context?.MaybeOwner is not { } owner) return;

            if (owner.Buffs.Enumerable
                .Where(b => b.IsTurnedOn)
                .SelectMany(b => b.Blueprint.ComponentsArray.OfType<EldritchBlastEssence>())
                .FirstOrDefault() is { } essence)
                this.SpellLevel = Math.Max(essence.EquivalentSpellLevel, this.BaseEquivalentSpellLevel);
            else this.SpellLevel = this.BaseEquivalentSpellLevel;
        }
    }
}
