using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;


namespace HomebrewWarlock.Features
{
    public static partial class EldritchBlast
    {
        public static BlueprintAbility AddOnHitEffectToAbility(BlueprintAbility ability)
        {
            ability.AddComponent<AbilityEffectRunAction>(c =>
            {
                c.Actions.Add(
                    GameActions.ContextActionDealDamage(action =>
                    {
                        action.m_Type = ContextActionDealDamage.Type.Damage;

                        action.DamageType.Type = DamageType.Energy;
                        action.DamageType.Energy = DamageEnergyType.Magic;

                        action.Value.DiceType = DiceType.D6;

                        action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                        action.Value.DiceCountValue.Value = 1;
                        action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
                    }));
            });

            ability.AddContextRankConfig(c =>
            {
                c.m_Type = AbilityRankType.DamageDice;
                c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                c.m_Feature = Feature.ToReference<BlueprintFeature, BlueprintFeatureReference>();
                c.m_Progression = ContextRankProgression.AsIs;
                c.m_StartLevel = 0;
                c.m_StepLevel = 1;
            });

            ability.AddComponent<ContextCalculateAbilityParams>(c =>
            {
                c.StatType = StatType.Charisma;
                c.ReplaceSpellLevel = true;
                c.SpellLevel = 1;
            });

            return ability;
        }
    }
}
