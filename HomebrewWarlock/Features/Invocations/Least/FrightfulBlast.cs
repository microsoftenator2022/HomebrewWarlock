using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class FrightfulBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Frightful Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 2" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a frightful blast. Any " +
            "creature struck by a frightful blast must succeed on a Will save or become shaken for 1 minute. A " +
            "shaken creature struck by a frightful blast is not affected by the shaken aspect of the blast but " +
            "takes damage normally. Creatures with immunity to mind-affecting spells and abilities or fear effects " +
            "cannot be shaken by a frightful blast.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("FrightfulBlastEssenceBuff"))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Shaken))
                .Map(bps =>
                {
                    var (buff, shaken) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 2;
                        c.Actions.Add(GameActions.Conditional(targetIsShaken =>
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
                                                applyBuff.m_Buff = shaken.ToReference();
                                                applyBuff.DurationValue.Rate = DurationRate.Minutes;
                                                applyBuff.DurationValue.BonusValue = 1;
                                            }))));
                                }));
                        }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("FrigthfulBlastAbility"))
                .Combine(essenceBuff)
                .Map(bps =>
                {
                    var (ability, essenceBuff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_Description;
                    ability.m_Icon = Sprites.FrightfulBlast;

                    ability.m_Buff = essenceBuff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("FrightfulBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Least_FrightfulBlast_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
