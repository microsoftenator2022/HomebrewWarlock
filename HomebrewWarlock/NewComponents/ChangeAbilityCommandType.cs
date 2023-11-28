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

namespace HomebrewWarlock.NewComponents
{
    [TypeId("35c97d31-c370-4366-83f8-78c40f7b663a")]
    internal class ChangeAbilityCommandType : UnitFactComponentDelegate
    {
        public UnitCommand.CommandType NewCommandType = UnitCommand.CommandType.Standard;
        public bool ToFullRoundAction = false;

        public UnitCommand.CommandType? RequireCommandType;

        public BlueprintAbilityReference[] Abilities = Array.Empty<BlueprintAbilityReference>();

        IEnumerable<BlueprintAbility> GetAbilityBlueprints() =>
            Abilities.Select(ability => ability.Get()).SkipIfNull();

        public override void OnTurnOn()
        {
            foreach (var ability in this.GetAbilityBlueprints())
            {
                var actionEntry = new UnitPartAbilityModifiers.ActionEntry(base.Fact, this.NewCommandType, ability)
                {
                    RequireFullRound = this.NewCommandType == UnitCommand.CommandType.Standard && ToFullRoundAction,
                    SpellCommandType = RequireCommandType
                };

                base.Owner.Ensure<UnitPartAbilityModifiers>().AddEntry(actionEntry);
            }
        }

        public override void OnTurnOff()
        {
            base.Owner.Ensure<UnitPartAbilityModifiers>().RemoveEntry(base.Fact);
        }
    }
}
