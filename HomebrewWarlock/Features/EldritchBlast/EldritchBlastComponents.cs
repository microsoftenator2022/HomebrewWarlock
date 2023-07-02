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

        //[Obsolete]
        //internal static (ContextActionDealDamage damageAction, ContextRankConfig contextRankConfig)
        //    GetEldritchBlastBaseDamageAndConfig(
        //    BlueprintFeatureReference ebRankFeature,
        //    DamageEnergyType damageType = DamageEnergyType.Magic)
        //{
        //    return
        //        (GetEldritchBlastDamage(damageType),
        //        GetContextRankConfig(ebRankFeature));
        //}

        //internal static BlueprintInitializationContext.ContextInitializer<BlueprintComponentList> BlastComponents(
        //    BlueprintInitializationContext context,
        //    BlueprintInitializationContext.ContextInitializer<BlueprintFeature> eldritchBlastRankFeature)
        //{
        //    return context.NewBlueprint<BlueprintComponentList>(GeneratedGuid.Get("EldritchBlastComponents"), nameof(GeneratedGuid.EldritchBlastComponents))
        //        .Combine(eldritchBlastRankFeature)
        //        .Map(((BlueprintComponentList, BlueprintFeature) bps) =>
        //        {
        //            var (bp, ebRankFeature) = bps;

        //            bp.AddComponent<ArcaneSpellFailureComponent>();

        //            bp.AddComponent<ContextRankConfig>(c =>
        //            {
        //                c.m_Type = AbilityRankType.DamageDice;
        //                c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
        //                c.m_Feature = ebRankFeature.ToReference<BlueprintFeatureReference>();
        //                c.m_Progression = ContextRankProgression.AsIs;
        //                c.m_StartLevel = 0;
        //                c.m_StepLevel = 1;
        //            });
                    

        //            return bp;
        //        });
        //}

        internal static ContextActionDealDamage GetEldritchBlastDamage(DamageEnergyType damageType = DamageEnergyType.Magic) =>
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
            conditionalEffects.ConditionsChecker.Operation = Operation.Or;

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

                conditionalEffects.Conditionals.Add(onHit);

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

    internal class ConditionalList : Conditional
    {
        public List<Conditional> Conditionals = new();

        public ConditionalList() : base()
        {
            ConditionsChecker ??= new();
            IfTrue ??= new();
            IfFalse ??= new();
        }

        public override void RunAction()
        {
            if (ConditionsChecker.Check())
            {
                var results = new List<(Conditional, bool result)>();

                foreach (var conditional in Conditionals)
                {
                    if (conditional.ConditionsChecker.Check())
                    {
                        results.Add((conditional, true));
                        conditional.IfTrue.Run();
                    }
                    else
                    {
                        results.Add((conditional, false));
                        conditional.IfFalse.Run();
                    }
                }

                switch (ConditionsChecker.Operation)
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
            }

            IfFalse.Run();
        }
    }

    internal class AbilityDeliverProjectileVariant : AbilityDeliverProjectile
    {
        //[HarmonyPatch(typeof(AbilityDeliverProjectile), nameof(AbilityDeliverProjectile.Projectiles), MethodType.Getter)]
        //static class get_Projectiles_Patch
        //{
        //    static bool Prefix(AbilityDeliverProjectile __instance,
        //        ref ReferenceArrayProxy<BlueprintProjectile, BlueprintProjectileReference> __result)
        //    {
        //        if (__instance is not AbilityDeliverProjectileVariant pv)
        //            return true;

        //        __result = pv.GetProjectiles().ToArray();
        //        return false;
        //    }
        //}

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
