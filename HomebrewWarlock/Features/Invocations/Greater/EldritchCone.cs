using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Features.EldritchBlast.Components;

using static HomebrewWarlock.Fx.FxColor;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.View;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Assets;
using MicroWrath.Util.Unity;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

    internal static class EldritchCone
    {
        class EldritchConeBlast() : BlastAbility(5)
        {
            public override ActionList DamageActions
            {
                get
                {
                    var damage = BaseDamage;
                    damage.HalfIfSaved = true;
                    damage.IsAoE = true;

                    return new()
                    {
                        Actions = new[] { damage }
                    };
                }
            }

            public override BlueprintAbility ConfigureAbility(BlueprintAbility ability, BlueprintFeatureReference rankFeature)
            {
                ability = base.ConfigureAbility(ability, rankFeature);

                var runAction = ability.GetComponent<AbilityEffectRunAction>();

                runAction.SavingThrowType = SavingThrowType.Reflex;

                return ability;
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Eldritch Cone";

        [LocalizedString]
        internal const string Description =
            "This blast shape invocation allows you to invoke your eldritch blast as a 30-foot cone. The eldritch " +
            "cone deals the normal eldritch blast damage to all targets within the area. This is not a ray attack, " +
            "so it requires no ranged touch attack. Any creature in the area of the cone can attempt a Reflex save " +
            "for half damage.";

        [LocalizedString]
        internal const string SavingThrow = "Reflex half";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var projectile = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.ChannelNegativeEnergyCone30Feet00,
                GeneratedGuid.Get("EldritchConeProjectile"),
                nameof(GeneratedGuid.EldritchConeProjectile))
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (projectile, baseFeatures) = bps;

                    static Color RotateColor(Color color)
                    {
                        color = UnityUtil.RotateColorHue(color, 120);

                        return color;
                    }

                    static Color AdjustValue(Color color)
                    {
                        var r = Mathf.Pow(color.r, 0.5f);
                        var g = Mathf.Pow(color.g, 0.5f);
                        var b = Mathf.Pow(color.b, 0.5f);

                        color = new(r, g, b, color.a);

                        color = color.ModifyHSV(hsv => hsv with { s = Math.Pow(hsv.s, 0.6) });

                        return color;
                    }

                    projectile.View = projectile.View.CreateDynamicMonobehaviourProxy<ProjectileView, ProjectileLink>(pv =>
                    {
                        pv.gameObject.name = "EldritchCone_projectile";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(pv.gameObject)}");

                        ChangeAllColors(pv.gameObject, RotateColor);
                        ChangeAllColors(pv.gameObject, AdjustValue);
                    });

                    projectile.CastFx = projectile.CastFx.CreateDynamicProxy(cfx =>
                    {
                        cfx.name = "EldritchCone_CastFX";

                        UnityEngine.Object.DestroyImmediate(cfx.transform.Find("CenterGlow/GreenSmoke").gameObject);

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(cfx)}");

                        ChangeAllColors(cfx, RotateColor);
                        ChangeAllColors(cfx, AdjustValue);

                        foreach (var al in cfx.GetComponentsInChildren<AnimatedLight>())
                        {
                            al.m_Intensity = al.m_Intensity * 5;
                        }

                        ChangeAllColors(cfx.transform.Find("Position_Offset/LastWave_Outer").gameObject, color =>
                        {
                            var r = Mathf.Pow(color.r, 2f);
                            var g = Mathf.Pow(color.g, 2f);
                            var b = Mathf.Pow(color.b, 2f);

                            color = new(r, g, b, color.a);

                            return color;
                        });
                    });

                    return projectile;
                });

            //var projectile = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.EnchantmentCone30Feet00,
            //    GeneratedGuid.Get("EldritchConeProjectile"),
            //    nameof(GeneratedGuid.EldritchConeProjectile))
            //    .Combine(baseFeatures)
            //    .Map(bps =>
            //    {
            //        var (projectile, baseFeatures) = bps;

            //        static Color RotateColor(Color color)
            //        {
            //            color = UnityUtil.RotateColorHue(color, -140);

            //            return color;
            //        }

            //        static Color ApplyAdjustment(Color color)
            //        {
            //            color = color.ModifyHSV(hsv =>
            //            {
            //                return hsv with { s = Math.Pow(hsv.s, 2) };
            //            });

            //            return color;
            //        }

            //        projectile.View = projectile.View.CreateDynamicMonobehaviourProxy<ProjectileView, ProjectileLink>(pv =>
            //        {
            //            pv.gameObject.name = "EldritchCone_projectile";

            //            MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(pv.gameObject)}");

            //            ChangeAllColors(pv.gameObject, RotateColor);
            //            ChangeAllColors(pv.gameObject, ApplyAdjustment);
            //        });

            //        projectile.CastFx = projectile.CastFx.CreateDynamicProxy(cfx =>
            //        {
            //            cfx.name = "EldritchCone_CastFX";

            //            MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(cfx)}");

            //            UnityEngine.Object.DestroyImmediate(cfx.transform.Find("Root/Smoke (3)").gameObject);
            //            UnityEngine.Object.DestroyImmediate(cfx.transform.Find("Root/Smoke (5)").gameObject);

            //            ChangeAllColors(cfx, RotateColor);
            //            ChangeAllColors(cfx, ApplyAdjustment);
            //        });

            //        return projectile;
            //    });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchConeAbility"),
                nameof(GeneratedGuid.EldritchConeAbility))
                .Combine(projectile)
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (ability, projectile, baseFeatures) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_EldritchCone_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_EldritchCone_Description;
                    ability.LocalizedSavingThrow = LocalizedStrings.Features_Invocations_Greater_EldritchCone_SavingThrow;

                    ability = new EldritchConeBlast()
                        .ConfigureAbility(ability, baseFeatures.rankFeature.ToReference<BlueprintFeatureReference>());

                    ability.GetComponent<AbilityEffectRunAction>().Actions.Add(new EldritchBlastOnHitFx()
                    {
                        DefaultProjectile = baseFeatures.projectile.ToReference<BlueprintProjectileReference>()
                    });

                    ability.AddComponent<DeliverEldritchBlastProjectile>(c =>
                    {
                        c.DefaultProjectile = projectile.ToReference<BlueprintProjectileReference>();

                        c.Type = AbilityProjectileType.Cone;

                        c.m_Length = new(30);
                        c.m_LineWidth = new(5);
                    });

                    ability.CanTargetFriends = true;
                    ability.CanTargetPoint = true;
                    ability.EffectOnAlly = AbilityEffectOnUnit.Harmful;
                    ability.Range = AbilityRange.Projectile;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchConeFeature"),
                nameof(GeneratedGuid.EldritchConeFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
