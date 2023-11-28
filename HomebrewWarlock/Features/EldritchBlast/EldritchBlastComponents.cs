using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;
using HomebrewWarlock.Fx;
using HomebrewWarlock.Features;
using HomebrewWarlock.NewComponents;

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

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal class BlastAbility(int equivalentSpellLevel)
    {
        public static ContextActionDealDamage BaseDamage =>
            GameActions.ContextActionDealDamage(action =>
            {
                action.m_Type = ContextActionDealDamage.Type.Damage;

                action.DamageType.Type = DamageType.Energy;
                action.DamageType.Energy = DamageEnergyType.Magic;

                action.Value.DiceType = DiceType.D6;

                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                action.Value.DiceCountValue.Value = 1;
                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;

                action.WriteRawResultToSharedValue = true;
                action.ResultSharedValue = AbilitySharedValue.Damage;
            });

        public readonly int EquivalentSpellLevel = equivalentSpellLevel;

        public virtual ActionList DamageActions => new() { Actions = new[] { BaseDamage } };

        public virtual BlueprintAbility ConfigureAbility(
            BlueprintAbility ability,
            BlueprintFeatureReference rankFeature) =>
            ConfigureAbility(ability, rankFeature,
                UnitCommand.CommandType.Standard);

        public virtual BlueprintAbility ConfigureAbility(
            BlueprintAbility ability,
            BlueprintFeatureReference rankFeature,
            UnitCommand.CommandType castTime)
        {
            ability.Type = AbilityType.SpellLike;

            ability.CanTargetEnemies = true;
            ability.SpellResistance = true;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
            ability.ActionType = castTime;

            ability.AvailableMetamagic =
                Metamagic.Empower |
                Metamagic.Maximize |
                Metamagic.Quicken;

            ability.AddComponent<EldritchBlastComponent>();
            ability.AddComponent(new EldritchBlastCalculateSpellLevel(EquivalentSpellLevel));

            ability.AddContextRankConfig(c =>
            {
                c.m_Type = AbilityRankType.DamageDice;
                c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                c.m_Feature = rankFeature;
                c.m_Progression = ContextRankProgression.AsIs;
                c.m_StartLevel = 0;
                c.m_StepLevel = 1;
            });

            var runAction = ability.EnsureComponent<AbilityEffectRunAction>();
            
            runAction.Actions.Add(DamageActions.Actions);
            runAction.Actions.Add(new EldritchBlastEssenceActions());

            //ability.AddContextRankConfig(crc =>
            //{
            //    crc.m_BaseValueType = ContextRankBaseValueType.MythicLevel;
            //    crc.m_Type = AbilityRankType.DamageDiceAlternative;
            //});

            //runAction.Actions.Add(GameActions.Conditional(c =>
            //{
            //    c.ConditionsChecker.Add(Conditions.ContextConditionCasterHasFact(casterHas =>
            //        casterHas.m_Fact = MythicBlast.CastBuffRef.ToReference<BlueprintUnitFact, BlueprintUnitFactReference>()));

            //    c.IfTrue.Add(GameActions.ContextActionDealDamage(damage =>
            //    {
            //        damage.DamageType.Type = DamageType.Energy;
            //        damage.DamageType.Energy = DamageEnergyType.Divine;

            //        damage.Value.DiceType = DiceType.D6;
            //        damage.Value.DiceCountValue.ValueType = ContextValueType.Rank;
            //        damage.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDiceAlternative;

            //        damage.Value.BonusValue.ValueType = ContextValueType.Rank;
            //        damage.Value.BonusValue.ValueRank = AbilityRankType.DamageDiceAlternative;
            //    }));
            //}));

            return ability;
        }
    }

    internal class EldritchBlastTouch(BlueprintItemWeaponReference touchWeapon, int equivalentSpellLevel = 1) : BlastAbility(equivalentSpellLevel)
    {
        public override BlueprintAbility ConfigureAbility(BlueprintAbility ability, BlueprintFeatureReference rankFeature)
        {
            ability = base.ConfigureAbility(ability, rankFeature);

            ability.Range = AbilityRange.Touch;

            ability.AddComponent<AbilityDeliverTouch>(c => c.m_TouchWeapon = touchWeapon);

            return ability;
        }
    }
}
