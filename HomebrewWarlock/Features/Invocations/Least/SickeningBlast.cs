using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Conditions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class SickeningBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Sickening Blast";

        [LocalizedString]
        internal const string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into a sickening blast. Any " +
            "living creature struck by a sickening blast must make a Fortitude save or become sickened for 1 " +
            "minute. A sickened creature struck by a second sickening blast is not affected by the sickening aspect " +
            "of the blast but still takes damage normally.";

        internal static BlueprintInitializationContext.ContextInitializer<EssenceFeature> Create(
            BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("SickeningBlastEssenceBuff"),
                nameof(GeneratedGuid.SickeningBlastEssenceBuff))
                .Map(buff =>
                {
                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("SickeningBlastAbility"),
                nameof(GeneratedGuid.SickeningBlastAbility))
                .Combine(essenceBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Sickened))
                .Map(bps =>
                {
                    var (ability, essenceBuff, debuff) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_SickeningBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Least_SickeningBlast_Description;
                    ability.m_Icon = Sprites.SickeningBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = Invocation.EssenceInvocationAbilityGroup;

                    Conditional onHit() => GameActions.Conditional(targetIsShaken =>
                    {
                        targetIsShaken.ConditionsChecker.Add(Conditions.ContextConditionHasBuffWithDescriptor(
                            condition =>
                            {
                                condition.SpellDescriptor = SpellDescriptor.Sickened;
                            }));

                        targetIsShaken.IfFalse.Add(
                            GameActions.ContextActionSavingThrow(savingThrow =>
                            {
                                savingThrow.Type = SavingThrowType.Fortitude;
                                savingThrow.Actions.Add(
                                    GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                        GameActions.ContextActionApplyBuff(applyBuff =>
                                        {
                                            applyBuff.m_Buff = debuff.ToReference<BlueprintBuffReference>();
                                            applyBuff.DurationValue.Rate = DurationRate.Minutes;
                                            applyBuff.DurationValue.BonusValue = 1;
                                        }))));
                            }));
                    });

                    return (ability, new EldritchBlastComponents.EssenceEffect(essenceBuff, () => new[] { onHit() }, 2));
                });

            var featureAndEssenceEffect = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("SickeningBlastFeature"),
                nameof(GeneratedGuid.SickeningBlastFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability, essenceEffect) = bps.Flatten();

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_SickeningBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Least_SickeningBlast_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return new EssenceFeature(feature, essenceEffect);
                });

            return featureAndEssenceEffect;
        }
    }
}
