using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Fx;

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

namespace HomebrewWarlock.NewActions
{

    [TypeId("6c89f79d-2ca3-4367-af50-60af8ce00121")]
    internal class CastSpellWithContextParams : ContextAction
    {
        public BlueprintAbilityReference? Spell;
        public bool MarkAsChild;

        public override string GetCaption() => $"Cast {Spell?.Get()}";
        public override void RunAction()
        {
            if (base.Context.MaybeCaster is not { } caster)
                return;
            if (base.Target.Unit is not { } target)
                return;

            var data = new AbilityData(this.Spell, caster);

            data.OverrideCasterLevel = base.Context.Params.CasterLevel;
            data.OverrideDC = base.Context.Params.DC;
            data.OverrideSpellLevel = base.Context.Params.SpellLevel;

            data.MetamagicData = new() { MetamagicMask = base.Context.Params.Metamagic };

            if (this.MarkAsChild)
                data.IsChildSpell = true;

            var rule = new RuleCastSpell(data, target);

            rule.IsDuplicateSpellApplied = base.AbilityContext?.IsDuplicateSpellApplied ?? false;

            Rulebook.Trigger(rule);
        }
    }
}
