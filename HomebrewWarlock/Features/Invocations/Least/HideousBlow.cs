using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Fx;
using HomebrewWarlock.NewActions;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.NewConditions;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UI.GenericSlot;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    using BaseBlastFeatures =
        (BlueprintFeature blastFeature,
        BlueprintFeature prerequisite,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);


    //internal class CastSpellWithContextParams : ContextAction
    //{
    //    public BlueprintAbilityReference? Spell;
    //    public bool MarkAsChild;

    //    public override string GetCaption() => $"Cast {Spell?.Get()}";
    //    public override void RunAction()
    //    {
    //        if (base.Context.MaybeCaster is not { } caster)
    //            return;
    //        if (base.Target.Unit is not { } target)
    //            return;

    //        var data = new AbilityData(this.Spell, caster);

    //        data.OverrideCasterLevel = base.Context.Params.CasterLevel;
    //        data.OverrideDC = base.Context.Params.DC;
    //        data.OverrideSpellLevel = base.Context.Params.SpellLevel;

    //        data.MetamagicData = new() { MetamagicMask = base.Context.Params.Metamagic };

    //        if (this.MarkAsChild)
    //            data.IsChildSpell = true;

    //        var rule = new RuleCastSpell(data, target);

    //        rule.IsDuplicateSpellApplied = base.AbilityContext?.IsDuplicateSpellApplied ?? false;

    //        Rulebook.Trigger(rule);
    //    }
    //}

    internal static class HideousBlow
    {
        [LocalizedString]
        internal const string DisplayName = "Hideous Blow";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Shape</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 1" +
            Environment.NewLine +
            "As a standard action, you can make a single melee attack. If you hit, the target is affected as if " +
            "struck by your eldritch blast (including any eldritch essence applied to the blast). This damage is in " +
            "addition to any weapon damage that you deal with your attack, although you need not deal damage with " +
            "this attack to trigger the eldritch blast effect.";

        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.HideousBlowAbility.ToMicroBlueprint<BlueprintAbility>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures,
            BlueprintInitializationContext.ContextInitializer<BlueprintAbility> ebTouch,
            BlueprintInitializationContext.ContextInitializer<BlueprintWeaponEnchantment> enchant)
        {
            //var enchant = context.NewBlueprint<BlueprintWeaponEnchantment>(
            //    GeneratedGuid.Get("HideousBlowWeaponEnchantment"))
            //    .Combine(ebTouch)
            //    .Combine(baseFeatures)
            //    .Map(bps =>
            //    {
            //        var (enchant, onHitAbility, baseFeatures) = bps.Expand();

            //        enchant.m_EnchantName = baseFeatures.baseFeature.m_DisplayName;

            //        enchant.WeaponFxPrefab = WeaponFxPrefabs.Standard;

            //        return enchant;
            //    });

            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("HideousBlowBuff"))
                .Combine(ebTouch)
                .Combine(enchant)
                .Map(bps =>
                {
                    (BlueprintBuff buff, var touch, var enchant) = bps.Expand();

                    buff.m_DisplayName = LocalizedStrings.Features_Invocations_Least_HideousBlow_DisplayName;
                    buff.m_Description = LocalizedStrings.Features_Invocations_Least_HideousBlow_Description;
                    buff.m_Icon = Sprites.HideousBlow;

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi | BlueprintBuff.Flags.IsFromSpell;

                    buff.AddComponent<BuffEnchantWornItem>(bewi =>
                    {
                        bewi.Slot = EquipSlotBase.SlotType.PrimaryHand;
                        bewi.m_EnchantmentBlueprint = enchant.ToReference<BlueprintItemEnchantmentReference>();
                    });

                    //buff.AddComponent<AddFactContextActions>(actions =>
                    //{
                    //    actions.Activated.Add(GameActions.ContextActionEnchantWornItem(a =>
                    //    {
                    //        a.ToCaster = true;
                    //        a.RemoveOnUnequip = true;
                    //        a.m_Enchantment = enchant.ToReference<BlueprintItemEnchantmentReference>();
                    //        a.Slot = EquipSlotBase.SlotType.PrimaryHand;
                    //        a.DurationValue.Rate = DurationRate.Rounds;
                    //        a.DurationValue.BonusValue.Value = 1;
                    //    }));
                    //});

                    //buff.AddReplaceAbilityParamsWithContext(c =>
                    //{
                    //    c.m_Ability = touch.ToReference();
                    //});

                    buff.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = true;
                        c.Action.Add(
                            //GameActions.ContextActionCastSpell(a =>
                            //{
                            //    a.m_Spell = touch.ToReference();
                            //    a.MarkAsChild = true;
                            //})
                            new CastSpellWithContextParams()
                            {
                                Spell = touch.ToReference(),
                                MarkAsChild = true
                            }
                            );
                    });

                    buff.AddComponent<AddInitiatorAttackWithWeaponTrigger>(c =>
                    {
                        c.OnlyHit = false;
                        c.Action.Add(GameActions.ContextActionRemoveSelf());
                    });

                    return buff;
                });

            var attackAbility = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowAttackAbility"))
                .Map(ability =>
                {
                    ability.AddComponent<AbilityDeliverAttackWithWeapon>();

                    ability.ActionType = UnitCommand.CommandType.Free;

                    ability.CanTargetEnemies = true;
                    ability.ShouldTurnToTarget = true;
                    ability.Hidden = true;

                    return ability;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("HideousBlowAbility"))
                .Combine(buff)
                .Combine(attackAbility)
                .Map(bps =>
                {
                    (BlueprintAbility ability, var buff, var attackAbility) = bps.Expand();

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
                        //GameActions.ContextActionEnchantWornItem(a =>
                        //{
                        //    a.ToCaster = true;
                        //    a.RemoveOnUnequip = true;
                        //    a.m_Enchantment = enchant.ToReference<BlueprintItemEnchantmentReference>();
                        //    a.Slot = EquipSlotBase.SlotType.PrimaryHand;
                        //    a.DurationValue.Rate = DurationRate.Rounds;
                        //    a.DurationValue.BonusValue.Value = 1;
                        //}),
                        GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.ToCaster = true;
                            ab.m_Buff = buff.ToReference();
                            ab.IsNotDispelable = true;
                            ab.DurationValue.BonusValue = 1;
                            ab.DurationValue.Rate = DurationRate.Rounds;
                        }),
                        GameActions.ContextActionCastSpell(a =>
                        {
                            a.m_Spell = attackAbility.ToReference();
                            a.MarkAsChild = true;
                        })));

                    ability.AddComponent<AbilityCasterMainWeaponIsMelee>();

                    ability.AddComponent<AbilityCasterHasNoFacts>(c =>
                    {
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

                    feature.AddPrerequisiteFeature(GeneratedGuid.EldritchBlastFeature.ToMicroBlueprint<BlueprintFeature>());

                    return feature;
                });
        }
    }
}
