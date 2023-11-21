using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Fx;
using HomebrewWarlock.Homebrew;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.Util.Linq;

namespace HomebrewWarlock.NewComponents
{
    [TypeId("fc311451-15fc-44dd-a13c-5c8e6540c785")]
    internal class AddDamageToBundle : UnitFactComponentDelegate, IInitiatorRulebookHandler<RulePrepareDamage>
    {
        public DamageTypeDescription DamageType = Default.DamageTypeDescription;

        public ContextDiceValue Value = Default.ContextDiceValue;

        public virtual void OnEventAboutToTrigger(RulePrepareDamage evt) { }

        public virtual void OnEventDidTrigger(RulePrepareDamage evt)
        {
            var damage = this.DamageType.CreateDamage(
                new DiceFormula(this.Value.DiceCountValue.Calculate(base.Context), this.Value.DiceType),
                this.Value.BonusValue.Calculate(base.Context));

            var fst = evt.DamageBundle.First();

            foreach (var dm in fst.Modifiers)
            {
                MicroLogger.Debug(() => $"{dm.Fact}, {dm.Value}, {dm.Descriptor}");
                damage.AddModifier(dm);
            }

            damage.CriticalModifier = fst.CriticalModifier;
            damage.CalculationType.Copy(fst.CalculationType);
            damage.AlignmentsMask = fst.AlignmentsMask;
            damage.Durability = fst.Durability;
            damage.EmpowerBonus.Copy(fst.EmpowerBonus);
            damage.BonusPercent = fst.BonusPercent;

            damage.SourceFact = base.Fact;

            evt.Add(damage);
        }
    }
}
