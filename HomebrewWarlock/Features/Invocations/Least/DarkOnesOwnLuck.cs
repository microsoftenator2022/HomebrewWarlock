using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class DarkOnesOwnLuck
    {
        internal static readonly ExtraActivatableAbilityGroup AbilityGroup = new(0x8fdde668);

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
                GeneratedGuid.Get("DarkOnesOwnLuckReflexBuff"));

            var reflexAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckReflexAbility"))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_DarkOnesOwnLuck_DisplayNameReflex;

                    return ability;
                })
                .Combine(reflexBuff)
                .Map(ab => new
                {
                    save = SavingThrowType.Reflex,
                    ability = ab.Left,
                    buff = ab.Right,
                    sprite = Sprites.DarkOnesOwnLuck.Reflex
                });


            var fortitudeBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DarkOnesOwnLuckFortitudeBuff"));

            var fortitudeAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckFortitudeAbility"))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_DarkOnesOwnLuck_DisplayNameFortitude;

                    return ability;
                })
                .Combine(fortitudeBuff)
                .Map(ab => new 
                {
                    save = SavingThrowType.Fortitude,
                    ability = ab.Left,
                    buff = ab.Right,
                    sprite = Sprites.DarkOnesOwnLuck.Fortitude
                });

            var willBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("DarkOnesOwnLuckWillBuff"));

            var willAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckWillAbility"))
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_DarkOnesOwnLuck_DisplayNameWill;

                    return ability;
                })
                .Combine(willBuff)
                .Map(ab => new
                {
                    save = SavingThrowType.Will,
                    ability = ab.Left,
                    buff = ab.Right,
                    sprite = Sprites.DarkOnesOwnLuck.Will
                });

            var baseAbility = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("DarkOnesOwnLuckBaseAbility"));

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("DarkOnesOwnLuckFeature"))
                .Combine(baseAbility)
                .Combine(fortitudeAbility)
                .Combine(reflexAbility)
                .Combine(willAbility)
                .Map(fs =>
                {
                    var (feature, baseAbility, f, r, w) = fs.Expand();
                    var abilities = new[] { f, r, w };

                    baseAbility.m_DisplayName = feature.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Least_DarkOnesOwnLuck_DisplayName;

                    baseAbility.m_Description = feature.m_Description =
                        LocalizedStrings.Features_Invocations_Least_DarkOnesOwnLuck_Description;

                    baseAbility.m_Icon = feature.m_Icon = Sprites.DarkOnesOwnLuck.Base;

                    foreach (var (save, buff, ability, sprite) in abilities.Select(x => (x.save, x.buff, x.ability, x.sprite)))
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

                        ability.ActivationType = AbilityActivationType.WithUnitCommand;
                        ability.m_ActivateWithUnitCommand = UnitCommand.CommandType.Standard;

                        ability.HiddenInUI = true;

                        ability.m_Icon = sprite;
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
