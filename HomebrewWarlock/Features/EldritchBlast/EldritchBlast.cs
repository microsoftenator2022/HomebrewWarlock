using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.Invocations;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

using UnityEngine;
using UnityEngine.UI;
using Kingmaker.Controllers.Projectiles;
using HomebrewWarlock.Resources;

namespace HomebrewWarlock.Features.EldritchBlast
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

    public static partial class EldritchBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Blast";

        [LocalizedString]
        internal static readonly string Description =
            "The first ability a warlock learns is eldritch blast. A warlock attacks his foes " +
            "with eldritch power, using baleful magical energy to deal damage and sometimes impart other " +
            $"debilitating effects.{Environment.NewLine}" +
            "An eldritch blast is a ray with a range of 60 feet. It is a ranged touch attack that affects a " +
            "single target, allowing no saving throw. An eldritch blast deals 1d6 points of damage at 1st level " +
            "and increases in power as the warlock rises in level. An eldritch blast is the equivalent of a " +
            "1st-level spell. If you apply a blast shape or eldritch essence invocation to your eldritch blast, " +
            $"your eldritch blast uses the level equivalent of the shape or essence.{Environment.NewLine}" +
            "An eldritch blast is subject to spell resistance, although the Spell Penetration feat and other " +
            "effects that improve caster level checks to overcome spell resistance also apply to eldritch " +
            "blast. An eldritch blast deals half damage to objects. Metamagic feats cannot improve a warlock's " +
            "eldritch blast (because it is a spell-like ability, not a spell). However, the feat Ability Focus " +
            "(eldritch blast) increases the DC for all saving throws (if any) associated with a warlock's " +
            "eldritch blast by 2.";

        [LocalizedString]
        internal const string ShortDescription =
            "A warlock attacks his foes with eldritch power, using baleful magical energy to deal damage and " +
            "sometimes impart other debilitating effects.";

        public static readonly IMicroBlueprint<BlueprintFeature> FeatureRef = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.EldritchBlastFeature);
        public static readonly IMicroBlueprint<BlueprintFeature> RankFeatureRef = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.EldritchBlastRank);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintAbility> CreateAbility(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> projectile,
            BlueprintInitializationContext.ContextInitializer<IEnumerable<EldritchBlastComponents.EssenceEffect>> essenceBuffs)
        {
            var ability = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.EldritchBlastAbility, nameof(GeneratedGuid.EldritchBlastAbility))
                .Combine(essenceBuffs)
                .Combine(projectile)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.RayItem))
                .Map(bps =>
                {
                    var (ability, essenceBuffs, projectile, rayItem) = bps.Expand();

                    ability.Type = AbilityType.Special;
                    ability.Range = AbilityRange.Close;

                    ability.CanTargetEnemies = true;
                    ability.SpellResistance = true;
                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
                    ability.ActionType = UnitCommand.CommandType.Standard;

                    EldritchBlastComponents.AddBlastComponents(
                        ability,
                        1,
                        RankFeatureRef.ToReference<BlueprintFeature, BlueprintFeatureReference>(),
                        essenceBuffs,
                        c =>
                        {
                            c.m_Projectiles = new[]
                            {
                                projectile.ToReference<BlueprintProjectileReference>()
                            };

                            c.m_Length = new();
                            c.m_LineWidth = new() { m_Value = 5 };

                            c.NeedAttackRoll = true;

                            c.m_Weapon = rayItem.ToReference<BlueprintItemWeaponReference>();
                        });

                    return ability;
                });

            return ability;
        }

        internal static BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures>
            CreateEldritchBlast(BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> projectile,
            BlueprintInitializationContext.ContextInitializer<IEnumerable<EldritchBlastComponents.EssenceEffect>> essenceEffects)
        {
            var ability = CreateAbility(context, projectile, essenceEffects);

            var rankFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastRank"), nameof(GeneratedGuid.EldritchBlastRank))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_EldritchBlast_ShortDescription;

                    feature.m_Icon = Sprites.EldritchBlast;

                    feature.Ranks = 9;

                    return feature;
                });

            var blast = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchBlastFeature"),
                nameof(GeneratedGuid.EldritchBlastFeature))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_EldritchBlast_ShortDescription;

                    feature.m_Icon = Sprites.EldritchBlast;
                    
                    return feature;
                })
                .Combine(rankFeature)
                .Combine(ability)
                .Combine(projectile)
                .Map(bps =>
                {
                    var (feature, rankFeature, ability, _) = bps.Expand();

                    ability.m_DisplayName = feature.m_DisplayName;
                    ability.m_Description = feature.m_Description;

                    ability.m_Icon = feature.m_Icon;

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = new[]
                        {
                            ability.ToReference<BlueprintUnitFactReference>(),
                            rankFeature.ToReference<BlueprintUnitFactReference>(),
                        };

                    });

                    return bps.Expand();
                });

            return blast;
        }
    }
}
