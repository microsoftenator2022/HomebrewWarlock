using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Fx;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UI.GenericSlot;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

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

        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.HideousBlowAbility.ToMicroBlueprint<BlueprintAbility>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures,
            BlueprintInitializationContext.ContextInitializer<BlueprintAbility> ebTouch)
        {
            var enchant = context.NewBlueprint<BlueprintWeaponEnchantment>(
                GeneratedGuid.Get("HideousBlowWeaponEnchantment"))
                .Combine(ebTouch)
                .Combine(baseFeatures)
                .Map(bps =>
                {
                    var (enchant, onHitAbility, baseFeatures) = bps.Expand();

                    enchant.m_EnchantName = baseFeatures.baseFeature.m_DisplayName;

                    enchant.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = true;
                        c.Action.Add(
                            GameActions.ContextActionCastSpell(a =>
                            {
                                a.m_Spell = onHitAbility.ToReference();
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
                GeneratedGuid.Get("HideousBlowAttackAbility"))
                .Map(ability =>
                {
                    ability.AddComponent<AbilityDeliverAttackWithWeapon>();

                    ability.CanTargetEnemies = true;
                    ability.ShouldTurnToTarget = true;
                    ability.Hidden = true;

                    return ability;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowAbility"))
                .Combine(enchant)
                .Combine(attackAbility)
                .Map(bps =>
                {
                    (BlueprintAbility ability, var enchant, var attackAbility) = bps.Expand();

                    attackAbility.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_DisplayName;

                    attackAbility.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Least_HideousBlow_Description;

                    attackAbility.m_Icon = ability.m_Icon = Sprites.HideousBlow;


                    ability.Type = AbilityType.Special;
                    ability.Range = AbilityRange.Weapon;

                    ability.CanTargetEnemies = true;

                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Immediate;

                    ability.ActionType = UnitCommand.CommandType.Standard;

                    ability.AddComponent<EldritchBlastComponent>();

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
                            a.m_Spell = attackAbility.ToReference())
                        ));

                    ability.AddComponent<AbilityCasterMainWeaponIsMelee>();

                    ability.AddComponent<AbilityCasterHasNoFacts>(c =>
                    {
                        // TODO: actually use the buff blueprint
                        c.m_Facts = new[] { GeneratedGuid.EldritchGlaiveBuff.ToBlueprintReference<BlueprintUnitFactReference>() };
                    });

                    ability.AddComponent<EldritchBlastCalculateSpellLevel>();

                    return ability;
                });

            return context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("HideousBlowFeature"))
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
