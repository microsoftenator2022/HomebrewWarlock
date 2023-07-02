using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.Invocations;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Components;
using MicroWrath.Conditions;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Util;

using UnityEngine;

namespace HomebrewWarlock.Features.EldritchBlast
{
    internal static class EldritchBlastComponents
    {
        internal static ContextRankConfig GetContextRankConfig(BlueprintFeatureReference ebRankFeature) =>
            new()
            {
                m_Type = AbilityRankType.DamageDice,
                m_BaseValueType = ContextRankBaseValueType.FeatureRank,
                m_Feature = ebRankFeature,
                m_Progression = ContextRankProgression.AsIs,
                m_StartLevel = 0,
                m_StepLevel = 1
            };

        private static ContextActionDealDamage GetEldritchBlastDamage(DamageEnergyType damageType = DamageEnergyType.Magic) =>
            GameActions.ContextActionDealDamage(action =>
            {
                action.m_Type = ContextActionDealDamage.Type.Damage;

                action.DamageType.Type = DamageType.Energy;
                action.DamageType.Energy = damageType;

                action.Value.DiceType = DiceType.D6;

                action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                action.Value.DiceCountValue.Value = 1;
                action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
            });

        internal record class EssenceEffect(
            BlueprintBuff EssenceBuff,
            Func<IEnumerable<GameAction>> OnHitActions,
            DamageEnergyType DamageType = DamageEnergyType.Magic,
            Func<AbilityProjectileType, BlueprintProjectileReference[]?>? Projectiles = null);

        internal static BlueprintAbility AddBlastComponents(
            BlueprintAbility ability,
            int equivalentSpellLevel,
            BlueprintFeatureReference ebRankFeature,
            IEnumerable<EssenceEffect> essenceEffects,
            Action<AbilityDeliverProjectile>? initProjectile = null)
        {
            ability.AddInvocationComponents(equivalentSpellLevel);

            ability.AddComponent(GetContextRankConfig(ebRankFeature));

            var runAction = ability.EnsureComponent<AbilityEffectRunAction>();

            var conditionalEffects = new ConditionalList();
            conditionalEffects.Operation = Operation.Or;

            conditionalEffects.IfFalse.Add(GetEldritchBlastDamage());

            AbilityDeliverProjectileVariant? projectiles = null;
            if (initProjectile is not null)
            {
                projectiles = ability.AddComponent<AbilityDeliverProjectileVariant>(initProjectile);
            }

            foreach (var ee in essenceEffects)
            {
                var buffCondition = Conditions.ContextConditionCasterHasFact(condition =>
                    condition.m_Fact = ee.EssenceBuff.ToReference<BlueprintUnitFactReference>());

                var onHit = GameActions.Conditional(casterHasBuff =>
                {
                    casterHasBuff.ConditionsChecker.Add(buffCondition);

                    casterHasBuff.IfTrue.Add(GetEldritchBlastDamage(ee.DamageType));
                    casterHasBuff.IfTrue.Add(ee.OnHitActions().ToArray());
                });

                conditionalEffects.Conditionals.Add((ee.EssenceBuff.ToReference<BlueprintBuffReference>(), onHit));

                if (projectiles is null) continue;
                if (ee.Projectiles is null || ee.Projectiles(projectiles.Type) is not { } p)
                    continue;

                var variant = new AbilityDeliverProjectileVariant.Variant();
                variant.ConditionsChecker.Add(buffCondition);
                variant.Projectiles = p;

                projectiles.Variants.Add(variant);
            }

            runAction.AddActions(conditionalEffects);

            return ability;
        }
    }

    internal class ConditionalList : GameAction
    {
        public List<(BlueprintBuffReference, Conditional)> Conditionals = new();

        public Operation Operation = Operation.Or;
        public ActionList IfTrue = new();
        public ActionList IfFalse = new();

        public override string GetCaption() => $"{nameof(ConditionalList)} ({Owner}, {name})";

        public ConditionalList() : base()
        {
        }

        public override void RunAction()
        {
            var results = new List<(Conditional, bool result)>();

            foreach (var (buff, conditional) in Conditionals)
            {
                if (conditional.ConditionsChecker.Check())
                {
                    MicroLogger.Debug(() => $"{buff} passed");

                    results.Add((conditional, true));
                    conditional.IfTrue.Run();
                }
                else
                {
                    MicroLogger.Debug(() => $"{buff} failed");

                    results.Add((conditional, false));
                    conditional.IfFalse.Run();
                }
            }

            switch (Operation)
            {
                case Operation.And:
                    if (results.Select(x => x.result).All(Functional.Identity))
                    {
                        IfTrue.Run();
                        return;
                    }
                    break;

                case Operation.Or:
                    if (results.Select(x => x.result).Any(Functional.Identity))
                    {
                        IfTrue.Run();
                        return;
                    }
                    break;
            }

            IfFalse.Run();
        }
    }

    internal class AbilityDeliverProjectileVariant : AbilityDeliverProjectile
    {
        public List<Variant> Variants = new();

        private BlueprintProjectileReference[]? defaultProjectiles;

        public BlueprintProjectileReference[] Default => defaultProjectiles ??= base.m_Projectiles;

        public BlueprintProjectileReference[] GetProjectiles() => Variants
            .Where(v => v.ConditionsChecker.Check())
            .Select(v => v.Projectiles)
            .FirstOrDefault() ?? Default;

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, Kingmaker.Utility.TargetWrapper target)
        {
            defaultProjectiles ??= base.m_Projectiles;

            m_Projectiles = GetProjectiles();

            return base.Deliver(context, target);
        }

        public class Variant
        {
            public ConditionsChecker ConditionsChecker = new();

            public BlueprintProjectileReference[]? Projectiles;
        }
    }
}
