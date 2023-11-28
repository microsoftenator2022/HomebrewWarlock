using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Util.Linq;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Dark
{
    using BaseBlastFeatures =
        (BlueprintFeature blastFeature,
        BlueprintFeature prerequisite,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);


    [HarmonyPatch]
    internal class EldritchDoomSpawnFx : AbilitySpawnFx
    {
        public DamageEnergyType? DamageType = null;

        [HarmonyPatch(typeof(AbilitySpawnFx), nameof(AbilitySpawnFx.DoSpawn))]
        [HarmonyPrefix]
        static bool DoSpawn_Prefix(AbilitySpawnFx __instance, AbilityExecutionContext context)
        {
            if (__instance is not EldritchDoomSpawnFx edsfx)
                return true;

            var energyTypes = context.Caster.Buffs.Enumerable
                .SelectMany(buff => buff.BlueprintComponents.OfType<EldritchBlastElementalEssence>())
                .Select(ebee => ebee.BlastDamageType);

            MicroLogger.Debug(sb =>
            {
                sb.AppendLine($"{nameof(EldritchDoomSpawnFx)}");
                sb.Append($"Is default? {edsfx.DamageType is null}");

                if (edsfx.DamageType is not null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"DamageType: {edsfx.DamageType}");
                    sb.Append($"Damage Buffs:");
                    
                    foreach (var e in energyTypes)
                    {
                        sb.AppendLine();
                        sb.Append($"{e}");
                    }
                }
            });

            if (edsfx.DamageType is null)
            {
                if (!energyTypes.Any())
                    return true;

                return false;
            }

            if (!energyTypes.Contains(edsfx.DamageType.Value))
                return false;

            return true;
        }
    }

    internal static class EldritchDoom
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Doom";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Shape</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 8" +
            Environment.NewLine +
            "This blast shape invocation allows you to invoke your eldritch blast as the dreaded eldritch doom. " +
            "This causes bolts of mystical power to lash out and savage nearby targets. An eldritch doom deals " +
            "eldritch blast damage to any number of targets designated by you and within 20 feet. This is not a ray " +
            "attack, so it requires no ranged touch attack. Each target can attempt a Reflex save for half damage.";

        [LocalizedString]
        internal const string LocalizedSavingThrow = "Reflex half";

        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.EldritchDoomAbility.ToMicroBlueprint<BlueprintAbility>();

        class EldritchDoomBlast() : BlastAbility(8)
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

            static PrefabLink DefaultFxPrefab =>
                (new PrefabLink() { AssetId = "d80d51c0a08f35140b10dd1526e540c4" })
                    .CreateDynamicProxy(fx =>
                    {
                        static Color RotateColor(Color color) => UnityUtil.RotateColorHue(color, -140);

                        fx.name = "EldritchDoom_Fx";

                        Fx.FxColor.ChangeAllColors(fx, RotateColor);

                        var rootTransform = fx.transform.Find("Root_Ground");

                        if (rootTransform == null)
                        {
                            MicroLogger.Error("Could not find 'Root_Ground' object");
                            return;
                        }

                        static void ScaleParticlesSizeOverTime(GameObject gameObject, float scale, bool includeChildren = false)
                        {
                            foreach (var particleSystem in includeChildren ?
                                gameObject.GetComponentsInChildren<ParticleSystem>().SkipIfNull() :
                                gameObject.GetComponent<ParticleSystem>().EmptyIfNull())
                            {
                                var sol = particleSystem.sizeOverLifetime;

                                var sizeM = sol.sizeMultiplier;
                                var xM = sol.xMultiplier;
                                var yM = sol.yMultiplier;
                                var zM = sol.zMultiplier;

                                sol.xMultiplier = xM * scale;
                                //sol.yMultiplier = yM * scale;
                                sol.zMultiplier = zM * scale;

                                //MicroLogger.Debug(() =>
                                //    $"{gameObject.name} " +
                                //    $"size: {sizeM} -> {sol.sizeMultiplier} " +
                                //    $"x: {xM} -> {sol.xMultiplier} " +
                                //    $"y: {yM} -> {sol.yMultiplier} " +
                                //    $"x: {zM} -> {sol.zMultiplier} ");
                            }
                        }

                        static void ScaleParticlesSizeInitial(GameObject gameObject, float scale, bool includeChildren = false)
                        {
                            foreach (var particleSystem in includeChildren ?
                                gameObject.GetComponentsInChildren<ParticleSystem>().SkipIfNull() :
                                gameObject.GetComponent<ParticleSystem>().EmptyIfNull())
                            {
                                var main = particleSystem.main;

                                var startSize = main.startSize;

                                var startConstant = startSize.constant;

                                var startSizeX = main.startSizeX;
                                var startSizeY = main.startSizeY;
                                var startSizeZ = main.startSizeZ;

                                var startConstantX = startSizeX.constant;
                                var startConstantY = startSizeY.constant;
                                var startConstantZ = startSizeZ.constant;

                                if (!main.startSize3D)
                                {
                                    startSize.constant = startConstant * scale;
                                    main.startSize = startSize;
                                }
                                else
                                {
                                    startSizeX.constant = startConstantX * scale;
                                    //startSizeY.constant = startConstantY * scale;
                                    startSizeZ.constant = startConstantZ * scale;

                                    main.startSizeX = startSizeX;
                                    main.startSizeY = startSizeY;
                                    main.startSizeZ = startSizeZ;
                                }

                                //MicroLogger.Debug(() => $"{gameObject.name} " +
                                //    $"start size: {startConstant} -> {particleSystem.main.startSize.constant} " +
                                //    $"start size X: {startConstantX} -> {particleSystem.main.startSizeX.constant} " +
                                //    $"start size Y: {startConstantY} -> {particleSystem.main.startSizeY.constant} " +
                                //    $"start size Z: {startConstantZ} -> {particleSystem.main.startSizeZ.constant} ");
                            }
                        }

                        foreach (var gameObject in new[]
                        {
                                        rootTransform.Find("Shockwave_Ring"),
                                    }
                        .SkipIfNull()
                        .Select(transform => transform.gameObject))
                        {
                            ScaleParticlesSizeOverTime(gameObject, 0.8f);

                            ScaleParticlesSizeInitial(gameObject, 0.8f);

                            var main = gameObject.GetComponent<ParticleSystem>().main;
                            main.startLifetimeMultiplier *= (1 / 0.8f);
                        }

                        foreach (var gameObject in new[]
                        {
                                        rootTransform.Find("MainWave"),
                                        rootTransform.Find("BottomSmoke"),
                                        rootTransform.Find("SmokeWave"),
                                        rootTransform.Find("DarkBottomSmoke"),
                                    }
                        .SkipIfNull()
                        .Select(transform => transform.gameObject))
                        {
                            ScaleParticlesSizeInitial(gameObject, 0.8f);

                            ScaleParticlesSizeOverTime(gameObject, 0.5f);
                        }

                        var lightT = rootTransform.Find("Point light (3)");

                        if (lightT is null)
                        {
                            MicroLogger.Error("Could not find 'Point light (3)'");
                            return;
                        }

                        var light = lightT.gameObject;

                        light.GetComponent<AnimatedLight>().m_Intensity *= 5;
                    });

            public override BlueprintAbility ConfigureAbility(BlueprintAbility ability, BlueprintFeatureReference rankFeature)
            {
                ability = base.ConfigureAbility(ability, rankFeature);
                ability.Range = AbilityRange.Personal;
                
                ability.CanTargetEnemies = false;
                ability.CanTargetFriends = false;
                ability.CanTargetSelf = true;

                ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Omni;

                var runAction = ability.GetComponent<AbilityEffectRunAction>();

                runAction.SavingThrowType = SavingThrowType.Reflex;

                ability.AddComponent<EldritchDoomSpawnFx>(c =>
                {
                    c.PrefabLink = DefaultFxPrefab;

                    c.Time = AbilitySpawnFxTime.OnApplyEffect;
                    c.Anchor = AbilitySpawnFxAnchor.Caster;
                });

                ability.AddComponent<EldritchDoomSpawnFx>(c =>
                {
                    c.DamageType = DamageEnergyType.Fire;

                    c.PrefabLink = new() { AssetId = "749ad3759dc93d64dba70a84d48135b5" };

                    c.Time = AbilitySpawnFxTime.OnApplyEffect;
                    c.Anchor = AbilitySpawnFxAnchor.Caster;
                });

                ability.AddComponent<EldritchDoomSpawnFx>(c =>
                {
                    c.DamageType = DamageEnergyType.Cold;

                    //kinetic_iceaoe00_20feet_aoe.fx
                    c.PrefabLink = new() { AssetId = "c591b2c6606714d4ebdf0c2fc05cafec" };

                    //kinetic_icesphereaoe00_20feet_aoe.fx
                    //c.PrefabLink = new() { AssetId = "b23302f818cab9a4f9b57b821195ed01" };

                    c.Time = AbilitySpawnFxTime.OnApplyEffect;
                    c.Anchor = AbilitySpawnFxAnchor.Caster;
                });

                ability.AddComponent<EldritchDoomSpawnFx>(c =>
                {
                    c.DamageType = DamageEnergyType.Acid;

                    //acidaoe20feet00_aoe.fx
                    //c.PrefabLink = new() { AssetId = "2e4bb367a72490b46944654f321b91a4" };

                    //causticeruption00_aoe.fx
                    c.PrefabLink = (new PrefabLink() { AssetId = "c483c82262bbae74a8a89d8e9fc97d81" })
                        .CreateDynamicProxy(fx =>
                        {
                            fx.transform.localScale = fx.transform.localScale * 2f / 3f;
                        });

                    c.Time = AbilitySpawnFxTime.OnApplyEffect;
                    c.Anchor = AbilitySpawnFxAnchor.Caster;
                });

                return ability;
            }
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchDoomAbility"))
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (ability, baseFeatures) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_EldritchDoom_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Dark_EldritchDoom_Description;
                    ability.LocalizedSavingThrow = LocalizedStrings.Features_Invocations_Dark_EldritchDoom_LocalizedSavingThrow;
                    ability.m_Icon = Sprites.EldritchDoom;

                    ability = new EldritchDoomBlast().ConfigureAbility(ability, baseFeatures.rankFeature.ToReference());

                    ability.GetComponent<AbilityEffectRunAction>().Actions.Add(new EldritchBlastOnHitFX()
                    {
                        DefaultProjectile = baseFeatures.projectile.ToReference()
                    });

                    ability.AddComponent<AbilityTargetsAround>(c =>
                    {
                        c.m_Radius = new Feet(20);
                        c.m_TargetType = TargetType.Enemy;
                    });

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchDoomFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    feature.AddPrerequisiteFeature(GeneratedGuid.EldritchBlastFeature.ToMicroBlueprint<BlueprintFeature>());

                    return feature;
                });

            return feature;
        }
    }
}
