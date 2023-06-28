﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Unity;

using UnityEngine;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class DarkOnesOwnLuck
    {
        internal const ActivatableAbilityGroup AbilityGroup = unchecked((ActivatableAbilityGroup)0x8fdde668);

        internal class SaveBonusComponent : UnitFactComponentDelegate
        {
            public SavingThrowType Save;

            internal BlueprintCharacterClassReference? classRef; 
            internal BlueprintCharacterClass? Class => classRef?.Get();

            public override void OnTurnOn()
            {
                base.OnTurnOn();

                if (Class is null) return;

                var cl = Owner.Progression.GetClassLevel(Class);
                var chaBonus = Owner.Stats.Charisma.Bonus;

                var bonusValue = Math.Min(cl, chaBonus);

                switch (Save)
                {
                    case SavingThrowType.Fortitude:
                        Owner.Stats.SaveFortitude.AddModifierUnique(bonusValue, Runtime, ModifierDescriptor.Luck);
                        break;

                    case SavingThrowType.Reflex:
                        Owner.Stats.SaveReflex.AddModifierUnique(bonusValue, Runtime, ModifierDescriptor.Luck);
                        break;

                    case SavingThrowType.Will:
                        Owner.Stats.SaveWill.AddModifierUnique(bonusValue, Runtime, ModifierDescriptor.Luck);
                        break;
                }
            }

            public override void OnTurnOff()
            {
                base.OnTurnOff();

                Owner.Stats.SaveFortitude.RemoveModifiersFrom(Runtime);
                Owner.Stats.SaveReflex.RemoveModifiersFrom(Runtime);
                Owner.Stats.SaveWill.RemoveModifiersFrom(Runtime);
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Dark One's Own Luck";

        [LocalizedString]
        internal const string DisplayNameReflex = "Dark One's Own Luck (Reflex)";

        [LocalizedString]
        internal const string DisplayNameWill = "Dark One's Own Luck (Will)";

        [LocalizedString]
        internal const string DisplayNameFortitude = "Dark One's Own Luck (Fortitude)";

        [LocalizedString]
        internal const string Description =
            "You are favored by the dark powers if you have this invocation. This invocation grants a luck bonus " +
            "equal to your Charisma bonus (if any) on Fortitude saves, Reflex saves, or Will saves. You can’t " +
            "apply this ability to two different save types at the same time. This bonus can never exceed your " +
            "class level.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var reflexBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DarkOnesOwnLuckReflexBuff"),
                nameof(GeneratedGuid.DarkOnesOwnLuckReflexBuff));

            var reflexAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckReflexAbility"),
                nameof(GeneratedGuid.DarkOnesOwnLuckReflexAbility))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_DarkOnesOwnLuck_DisplayNameReflex;

                    return ability;
                })
                .Combine(reflexBuff)
                .Map(ab => new
                {
                    save = SavingThrowType.Reflex,
                    ability = ab.Left,
                    buff = ab.Right,
                    spriteOverlay =
                        AssetUtils.GetSpriteAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $"{nameof(HomebrewWarlock)}.Resources.dol_icon_Ref.png")
                });


            var fortitudeBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DarkOnesOwnLuckFortitudeBuff"),
                nameof(GeneratedGuid.DarkOnesOwnLuckFortitudeBuff));

            var fortitudeAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckFortitudeAbility"),
                nameof(GeneratedGuid.DarkOnesOwnLuckFortitudeAbility))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_DarkOnesOwnLuck_DisplayNameFortitude;

                    return ability;
                })
                .Combine(fortitudeBuff)
                .Map(ab => new 
                {
                    save = SavingThrowType.Fortitude,
                    ability = ab.Left,
                    buff = ab.Right,
                    spriteOverlay =
                        AssetUtils.GetSpriteAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $"{nameof(HomebrewWarlock)}.Resources.dol_icon_Fort.png")
                });

            var willBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DarkOnesOwnLuckWillBuff"),
                nameof(GeneratedGuid.DarkOnesOwnLuckWillBuff));

            var willAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckWillAbility"),
                nameof(GeneratedGuid.DarkOnesOwnLuckWillAbility))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_DarkOnesOwnLuck_DisplayNameWill;

                    return ability;
                })
                .Combine(willBuff)
                .Map(ab => new
                {
                    save = SavingThrowType.Will,
                    ability = ab.Left,
                    buff = ab.Right,
                    spriteOverlay =
                        AssetUtils.GetSpriteAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $"{nameof(HomebrewWarlock)}.Resources.dol_icon_Will.png")
                });

            var baseAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckBaseAbility"),
                nameof(GeneratedGuid.DarkOnesOwnLuckBaseAbility));

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("DarkOnesOwnLuckFeature"),
                nameof(GeneratedGuid.DarkOnesOwnLuckFeature))
                .Combine(baseAbility)
                .Combine(fortitudeAbility)
                .Combine(reflexAbility)
                .Combine(willAbility)
                .Map(fs =>
                {
                    var (feature, baseAbility, f, r, w) = fs.Expand();
                    var abilities = new[] { f, r, w };

                    baseAbility.m_DisplayName = feature.m_DisplayName =
                        LocalizedStrings.Features_Invocations_DarkOnesOwnLuck_DisplayName;

                    baseAbility.m_Description = feature.m_Description =
                        LocalizedStrings.Features_Invocations_DarkOnesOwnLuck_Description;

                    baseAbility.m_Icon = feature.m_Icon =
                        AssetUtils.GetSpriteAssemblyResource(
                            Assembly.GetExecutingAssembly(),
                            $"{nameof(HomebrewWarlock)}.Resources.dool_icon.png");

                    foreach (var (save, buff, ability, spriteOverlay) in abilities.Select(x => (x.save, x.buff, x.ability, x.spriteOverlay)))
                    {
                        buff.m_Description = ability.m_Description = feature.m_Description;

                        buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                        buff.AddComponent<SaveBonusComponent>(c =>
                        {
                            c.classRef = WarlockClass.Blueprint
                                .ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>();
                            c.Save = save;
                        });

                        ability.Group = AbilityGroup;

                        ability.m_Buff = buff.ToReference<BlueprintBuffReference>();

                        ability.ActivationType = AbilityActivationType.Immediately;

                        ability.HiddenInUI = true;

                        ability.m_Icon = spriteOverlay;

                        //ability.AddComponent<UI.ForeIcon>(c => c.Icon = spriteOverlay);
                    }

                    baseAbility.AddActivatableAbilityVariants(c =>
                    {
                        c.m_Variants = abilities.Select(x => x.ability.ToReference<BlueprintActivatableAbilityReference>()).ToArray();
                    });

                    baseAbility.AddActivationDisable();

                    feature.AddAddFacts(c =>
                    {
                        //c.m_Facts = abilities.Select(x => x.ability.ToReference<BlueprintUnitFactReference>()).ToArray();
                        c.m_Facts = new[] { baseAbility.ToReference<BlueprintUnitFactReference>() };
                    });

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = abilities.Select(x => x.ability.ToReference<BlueprintUnitFactReference>()).ToArray();
                    });

                    return feature;
                });

            return feature;
        }
    }
}