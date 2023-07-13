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
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Conditions;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class FrightfulBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Frightful Blast";

        [LocalizedString]
        internal const string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into a frightful blast. Any " +
            "creature struck by a frightful blast must succeed on a Will save or become shaken for 1 minute. A " +
            "shaken creature struck by a frightful blast is not affected by the shaken aspect of the blast but " +
            "takes damage normally. Creatures with immunity to mind-affecting spells and abilities or fear effects " +
            "cannot be shaken by a frightful blast.";

        internal static BlueprintInitializationContext.ContextInitializer<EssenceFeature> Create(
            BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("FrightfulBlastEssenceBuff"),
                nameof(GeneratedGuid.FrightfulBlastEssenceBuff))
                .Map(buff =>
                {
                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("FrigthfulBlastAbility"),
                nameof(GeneratedGuid.FrigthfulBlastAbility))
                .Combine(essenceBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Shaken))
                .Map(bps =>
                {
                    var (ability, essenceBuff, debuff) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_Description;
                    ability.m_Icon = Sprites.FrightfulBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = Invocation.EssenceInvocationAbilityGroup;

                    Conditional onHit() => GameActions.Conditional(targetIsShaken =>
                    {
                        targetIsShaken.ConditionsChecker.Add(Conditions.ContextConditionHasBuffWithDescriptor(
                            condition =>
                            {
                                condition.SpellDescriptor = SpellDescriptor.Shaken;
                            }));

                        targetIsShaken.IfFalse.Add(
                            GameActions.ContextActionSavingThrow(savingThrow =>
                            {
                                savingThrow.Type = SavingThrowType.Will;
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
                GeneratedGuid.Get("FrightfulBlastFeature"),
                nameof(GeneratedGuid.FrightfulBlastFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability, essenceEffect) = bps.Flatten();

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return new EssenceFeature(feature, essenceEffect);
                });

            return featureAndEssenceEffect;
        }
    }
}
