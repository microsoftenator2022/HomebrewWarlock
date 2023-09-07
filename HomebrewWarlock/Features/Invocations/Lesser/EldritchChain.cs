using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    using BaseBlastFeatures =
    (BlueprintFeature baseFeature,
    BlueprintFeature rankFeature,
    BlueprintAbility baseAbility,
    BlueprintProjectile projectile);

    internal static class EldritchChain
    {
        [TypeId("d04940b7-4f0d-4282-b7b6-7bcc59fbc9ef")]
        internal class DeliverEldritchChain : AbilityDeliverChain
        {
            public BlueprintProjectileReference? DefaultProjectile;
            public BlueprintProjectileReference? DefaultProjectileFirst;

            public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
            {
                base.m_Projectile = DefaultProjectile ?? DefaultProjectileFirst;
                base.m_ProjectileFirst = DefaultProjectileFirst ?? DefaultProjectile;

                if (context is not null && context.MaybeCaster is { } caster)
                {
                    var essenceProjectiles = EldritchBlastEssence.GetEssenceBuffs(caster)
                        .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastEssence>())
                        .Select(c => c.Projectiles)
                        .FirstOrDefault(p => p.ContainsKey(AbilityProjectileType.Simple));

                    if (essenceProjectiles is not null)
                    {
                        base.m_ProjectileFirst = essenceProjectiles[AbilityProjectileType.Simple].First();
                        base.m_Projectile = essenceProjectiles[AbilityProjectileType.Simple].Last();
                    }
                }

                return base.Deliver(context, target);
            }
        }

        internal class EldritchChainBlastAbility() : BlastAbility(4)
        {
            public override ActionList DamageActions => new()
            {
                Actions = new[] { GameActions.Conditional(conditional =>
                {
                    conditional.ConditionsChecker.Add(Conditions.ContextConditionIsMainTarget());
                    conditional.IfTrue.Add(BaseDamage, new EldritchBlastEssenceActions());

                    var halfDamage = BaseDamage;
                    halfDamage.Half = true;

                    conditional.IfFalse.Add(halfDamage, new EldritchBlastEssenceActions());
                }) }
            };

            public override BlueprintAbility ConfigureAbility(BlueprintAbility ability, BlueprintFeatureReference rankFeature)
            {
                ability = base.ConfigureAbility(ability, rankFeature);

                ability.Range = AbilityRange.Close;
                
                ability.AddComponent<DeliverEldritchChain>(c =>
                {
                    c.Radius = new Feet(30);
                });

                var runAction = ability.GetComponent<AbilityEffectRunAction>();

                runAction.Actions.Actions = runAction.Actions.Actions.Where(a => a is not EldritchBlastEssenceActions).ToArray();

                return ability;
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Eldritch Chain";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Shape</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 4" +
            Environment.NewLine +
            "This blast shape invocation allows you to improve your eldritch blast by turning it into an arc of " +
            "energy that “jumps” from the first target to others. An eldritch chain can jump to one or more " +
            "secondary targets within 30 feet of the first target, allowing you to make additional ranged touch " +
            "attacks and deal damage to the secondary targets if you hit." + Environment.NewLine +
            "You can “jump” the chain to one secondary target per five caster levels, so you can strike two " +
            "additional targets at 10th level, three additional targets at 15th level, and four additional targets " +
            "at 20th level. Each new target must be within 30 feet of the previous one, and you can’t target the " +
            "same creature more than once with the eldritch chain. If you miss any target in the chain, the " +
            "eldritch chain attack ends there." + Environment.NewLine +
            "Each target struck after the first takes half the damage dealt to the first target. " +
            //"This reduction in damage to secondary targets applies to any effect that increases the damage of your " +
            //"eldritch blast (such as vitriolic blast)." +
            Environment.NewLine +
            "You must make a separate spell penetration check for each target, if applicable.";

        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.EldritchChainAbility.ToMicroBlueprint<BlueprintAbility>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateBlast(
           BlueprintInitializationContext context,
           BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var rankFeature = baseFeatures.Map(bf => bf.rankFeature);

            var projectile = baseFeatures.Map(bf => bf.projectile);

            var secondaryProjectile = projectile.Map(baseProjectile =>
            {
                var projectile = AssetUtils.CloneBlueprint(baseProjectile,
                    GeneratedGuid.Get("EldritchChainSecondaryProjectile"));

                projectile.SourceBone = "Locator_HitFX_00";

                projectile.CastFx.AssetId = null;

                return projectile;
            });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchChainAbility"))
                .Combine(rankFeature)
                .Combine(projectile)
                .Combine(secondaryProjectile)
                .Map(bps =>
                {
                    var (ability, rankFeature, projectile, secondProjectile) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_EldritchChain_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Lesser_EldritchChain_Description;
                    ability.m_Icon = Sprites.ChainLightning;

                    ability = new EldritchChainBlastAbility().ConfigureAbility(ability, rankFeature.ToReference());

                    var chain = ability.GetComponent<DeliverEldritchChain>();

                    chain.TargetsCount.ValueType = ContextValueType.Rank;
                    chain.TargetsCount.ValueRank = AbilityRankType.ProjectilesCount;

                    chain.DefaultProjectileFirst = projectile.ToReference();
                    chain.DefaultProjectile = secondProjectile.ToReference();

                    ability.AddContextRankConfig(c =>
                    {
                        c.m_Type = AbilityRankType.ProjectilesCount;

                        c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                        c.m_Class = new[] { WarlockClass.Blueprint.ToReference() };

                        c.m_Progression = ContextRankProgression.OnePlusDivStep;
                        c.m_StepLevel = 5;

                        c.m_Min = 2;
                        c.m_UseMin = true;
                    });

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchChainFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
