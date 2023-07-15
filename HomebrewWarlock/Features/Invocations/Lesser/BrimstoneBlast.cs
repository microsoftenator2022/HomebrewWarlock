﻿using System;
using System.Linq;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.View;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Assets;
using MicroWrath.Util.Unity;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class BrimstoneBlast
    {
        internal static BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> CreateProjectile(BlueprintInitializationContext context)
        {
            var projectile = context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00)
                .Map((BlueprintProjectile bp) =>
                {
                    static Color RotateColor(Color color) => UnityUtil.RotateColorHue(color, -110);

                    bp = AssetUtils.CloneBlueprint(bp, GeneratedGuid.Get("BrimstoneBlastProjectile"), nameof(GeneratedGuid.BrimstoneBlastProjectile));

                    bp.View = bp.View.CreateDynamicMonobehaviourProxy<ProjectileView, ProjectileLink>(pv =>
                    {
                        pv.gameObject.name = "BrimstoneBlast_projectile";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(pv.gameObject)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(pv.gameObject, RotateColor);
                    });

                    bp.CastFx = bp.CastFx.CreateDynamicProxy(cfx =>
                    {
                        cfx.name = "BrimstoneBlast_CastFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(cfx)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(cfx, RotateColor);
                    });

                    bp.ProjectileHit.HitFx = bp.ProjectileHit.HitFx.CreateDynamicProxy(hfx =>
                    {
                        hfx.name = "BrimstoneBlast_HitFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(hfx)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(hfx, RotateColor);
                    });

                    bp.ProjectileHit.HitSnapFx = bp.ProjectileHit.HitSnapFx.CreateDynamicProxy(hsfx =>
                    {
                        hsfx.name = "BrimstoneBlast_HitSnapFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(hsfx)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(hsfx, RotateColor);
                    });

                    bp.ProjectileHit.MissFx = bp.ProjectileHit.MissFx.CreateDynamicProxy(mfx =>
                    {
                        mfx.name = "BrimstoneBlast_MissFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(mfx)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(mfx, RotateColor);
                    });

                    bp.ProjectileHit.MissDecalFx = bp.ProjectileHit.MissDecalFx.CreateDynamicProxy(mdfx =>
                    {
                        mdfx.name = "BrimstoneBlast_MissDecalFX";

                        MicroLogger.Debug(() => $"{UnityUtil.Debug.DumpGameObject(mdfx)}");

                        EldritchBlast.EldritchBlast.ChangeAllColors(mdfx, RotateColor);
                    });

                    return bp;
                });

            return projectile;
        }

        [LocalizedString]
        internal const string DisplayName = "Brimstone Blast";

        [LocalizedString]
        internal static readonly string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into a brimstone blast. A " +
            "brimstone blast deals fire damage." + Environment.NewLine +
            "Any creature struck by a brimstone blast must succeed on a Reflex save or catch on fire, taking 2d6 " +
            "points of fire damage per round until " +
            //"it takes a full-round action to extinguish the flames or " +
            "the duration expires. The fire damage persists for 1 round per five class levels you have." +
            Environment.NewLine + "For example, a 15th-level warlock deals 2d6 points of fire damage for 3 rounds " +
            "after the initial brimstone blast attack. A creature burning in this way never takes more than 2d6 " +
            "points of fire damage in a round, even if it has been hit by more than one brimstone blast.";

        internal static BlueprintInitializationContext.ContextInitializer<EssenceFeature> Create(
            BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BrimstoneBlastEssenceBuff"),
                nameof(GeneratedGuid.BrimstoneBlastEssenceBuff))
                .Map(buff =>
                {
                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var dotBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BrimstoneBlastPerRoundDamage"),
                nameof(GeneratedGuid.BrimstoneBlastPerRoundDamage))
                .Map(buff =>
                {
                    buff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.NewRound.Add(GameActions.ContextActionDealDamage(a =>
                        {
                            a.DamageType.Type = DamageType.Energy;
                            a.DamageType.Energy = DamageEnergyType.Fire;
                            a.Value.DiceType = DiceType.D6;
                            a.Value.DiceCountValue = 2;
                        }));
                    });

                    buff.m_Flags = BlueprintBuff.Flags.Harmful;

                    return buff;
                });

            var applyDotBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BrimstoneBlastApplyPerRoundDamage"),
                nameof(GeneratedGuid.BrimstoneBlastApplyPerRoundDamage))
                .Combine(dotBuff)
                .Map(bps =>
                {
                    var (dotBuff, applyBuff) = bps;

                    applyBuff.AddActionsOnBuffApply(c => c.Actions.Add(GameActions.ContextActionApplyBuff(ab =>
                    {
                        ab.m_Buff = dotBuff.ToReference<BlueprintBuffReference>();
                        ab.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
                        ab.IsNotDispelable = true;
                    })));

                    applyBuff.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                        c.m_Class = new []
                        {
                            WarlockClass.Blueprint.ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>()
                        };
                        c.m_Progression = ContextRankProgression.DelayedStartPlusDivStep;
                        c.m_StepLevel = 5;
                    });

                    applyBuff.Stacking = StackingType.Stack;

                    applyBuff.m_Flags = BlueprintBuff.Flags.Harmful | BlueprintBuff.Flags.HiddenInUi;

                    return applyBuff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("BrimstonBlastAbility"),
                nameof(GeneratedGuid.BrimstonBlastAbility))
                .Combine(buff)
                .Combine(dotBuff)
                .Combine(applyDotBuff)
                //.Combine(CreateProjectile(context))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.HellfireRay00))
                .Map(bps =>
                {
                    var (ability, essenceBuff, dotBuff, applyBuff, projectile) = bps.Expand();

                    applyBuff.m_DisplayName = dotBuff.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_BrimstoneBlast_DisplayName;

                    applyBuff.m_Description = dotBuff.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Lesser_BrimstoneBlast_Description;

                    dotBuff.m_Icon = ability.m_Icon = Sprites.BrimstoneBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = Invocation.EssenceInvocationAbilityGroup;

                    ContextActionSavingThrow onHit() => GameActions.ContextActionSavingThrow(savingThrow =>
                    {
                        savingThrow.Type = SavingThrowType.Reflex;
                        savingThrow.Actions.Add(GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                            GameActions.ContextActionApplyBuff(ab =>
                            {
                                ab.m_Buff = applyBuff.ToReference<BlueprintBuffReference>();
                                ab.DurationValue.BonusValue = 1;
                            }))));
                    });

                    return (ability, new EldritchBlastComponents.EssenceEffect(
                    essenceBuff,
                    () => new[] { onHit() },
                    3,
                    DamageEnergyType.Fire,
                    new []
                    { 
                        (AbilityProjectileType.Simple,
                        new [] { projectile.ToReference<BlueprintProjectileReference>() })
                    }
                    ));
                });

            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("BrimstoneBlastFeature"),
                nameof(GeneratedGuid.BrimstoneBlastFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability, essenceEffect) = bps.Flatten();

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return new EssenceFeature(feature, essenceEffect);
                });
        }
    }
}
