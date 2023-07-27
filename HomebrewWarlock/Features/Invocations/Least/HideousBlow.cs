using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Features.EldritchBlast.Components;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ResourceLinks;
using Kingmaker.UI.GenericSlot;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Components;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Assets;
using MicroWrath.Util.Unity;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations.Least
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

    internal static class WeaponFxPrefabs
    {
        internal static PrefabLink Standard =>
            new PrefabLink() { AssetId = "10e570e1da0d99f4ab69893791b17af4" }.CreateDynamicProxy(fx =>
            {
                var lightning = fx.transform.Find("Lightning")?.gameObject;
                UnityEngine.Object.DestroyImmediate(lightning);

                var sparks = fx.transform.Find("Sparks")?.gameObject;
                UnityEngine.Object.DestroyImmediate(sparks);

                var eod = fx.transform.Find("ElectricityOverDistance")?.gameObject;
                UnityEngine.Object.DestroyImmediate(eod);

                var ek = fx.transform.Find("ElectroKatyshki")?.gameObject;
                UnityEngine.Object.DestroyImmediate(ek);

                var electricity = fx.transform.Find("Electricity")?.gameObject;
                UnityEngine.Object.DestroyImmediate(electricity);

                EldritchBlast.EldritchBlast.ChangeAllColors(fx, c =>
                {
                    UnityEngine.Color.RGBToHSV(UnityUtil.RotateColorHue(c, 20), out var h, out var s, out var v);

                    return UnityEngine.Color.HSVToRGB(h, Mathf.Clamp01((float)(s * 1.15)), (float)(v * 0.85));
                });
            });
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

    internal static class HideousBlow
    {
        [LocalizedString]
        internal const string DisplayName = "Hideous Blow";

        [LocalizedString]
        internal const string Description =
            "As a standard action, you can make a single melee attack. If you hit, the target is affected as if " +
            "struck by your eldritch blast (including any eldritch essence applied to the blast). This damage is in " +
            "addition to any weapon damage that you deal with your attack, although you need not deal damage with " +
            "this attack to trigger the eldritch blast effect.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var onHitAbility = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowOnHitAbility"),
                nameof(GeneratedGuid.HideousBlowOnHitAbility))
                .Combine(baseFeatures)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.TouchItem))
                .Map(bps =>
                {
                    var (ability, baseFeatures, touchWeapon) = bps.Expand();

                    ability.m_DisplayName = baseFeatures.baseFeature.m_DisplayName;
                    ability.m_Description = baseFeatures.baseFeature.m_Description;

                    ability = new EldritchBlastTouch(touchWeapon.ToReference<BlueprintItemWeaponReference>())
                        .ConfigureAbility(ability, baseFeatures.rankFeature.ToReference<BlueprintFeatureReference>());

                    return ability;
                });

            var enchant = context.NewBlueprint<BlueprintWeaponEnchantment>(
                GeneratedGuid.Get("HideousBlowWeaponEnchantment"),
                nameof(GeneratedGuid.HideousBlowWeaponEnchantment))
                .Combine(onHitAbility)
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (enchant, onHitAbility, baseFeatures) = bps.Expand();

                    enchant.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = true;
                        c.Action.Add(
                            GameActions.ContextActionCastSpell(a =>
                            {
                                a.m_Spell = onHitAbility.ToReference<BlueprintAbilityReference>();
                            }));
                    });

                    enchant.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = false;
                        c.Action.Add(new EnchantmentRemoveSelf());
                    });

                    enchant.WeaponFxPrefab = WeaponFxPrefabs.Standard;

                    return enchant;
                });

            var attackAbility = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowAttackAbility"),
                nameof(GeneratedGuid.HideousBlowAttackAbility))
                .Map(ability =>
                {
                    ability.AddComponent<AbilityDeliverAttackWithWeapon>();

                    ability.CanTargetEnemies = true;
                    ability.ShouldTurnToTarget = true;
                    ability.Hidden = true;

                    return ability;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowAbility"),
                nameof(GeneratedGuid.HideousBlowAbility))
                .Combine(enchant)
                .Combine(attackAbility)
                .Map(bps =>
                {
                    var (ability, enchant, attackAbility) = bps.Expand();

                    attackAbility.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_DisplayName;

                    attackAbility.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_Description;

                    ability.Type = AbilityType.Special;
                    ability.Range = AbilityRange.Weapon;

                    ability.CanTargetEnemies = true;

                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;

                    ability.ActionType = UnitCommand.CommandType.Standard;

                    ability.AddComponent<ArcaneSpellFailureComponent>();

                    ability.AddComponent<AbilityEffectRunAction>(c => c.AddActions(
                        GameActions.ContextActionEnchantWornItem(a =>
                        {
                            a.ToCaster = true;
                            a.RemoveOnUnequip = true;
                            a.m_Enchantment = enchant.ToReference<BlueprintItemEnchantmentReference>();
                            a.Slot = EquipSlotBase.SlotType.PrimaryHand;
                            a.DurationValue.Rate = DurationRate.Rounds;
                            a.DurationValue.BonusValue.Value = 1;
                        }),
                        GameActions.ContextActionCastSpell(a =>
                            a.m_Spell = attackAbility.ToReference<BlueprintAbilityReference>())
                        ));

                    ability.AddComponent<AbilityCasterMainWeaponIsMelee>();

                    ability.AddComponent<EldritchBlastCalculateSpellLevel>();

                    return ability;
                });

            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("HideousBlowFeature"),
                nameof(GeneratedGuid.HideousBlowFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_DisplayName;

                    feature.m_Description =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_Description;

                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c =>
                        c.m_Facts = new[]
                        {
                            ability.ToReference<BlueprintUnitFactReference>()
                        });

                    return feature;
                });
        }
    }
}
