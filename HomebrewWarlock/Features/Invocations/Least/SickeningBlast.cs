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

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("SickeningBlastEssenceBuff"))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Sickened))
                .Map(bps =>
                {
                    var (buff, sickened) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 2;

                        c.Actions.Add(GameActions.Conditional(targetIsSickened =>
                        {
                            targetIsSickened.ConditionsChecker.Add(Conditions.ContextConditionHasBuffWithDescriptor(
                                condition =>
                                {
                                    condition.SpellDescriptor = SpellDescriptor.Sickened;
                                }));

                            targetIsSickened.IfFalse.Add(
                                GameActions.ContextActionSavingThrow(savingThrow =>
                                {
                                    savingThrow.Type = SavingThrowType.Fortitude;
                                    savingThrow.Actions.Add(
                                        GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                            GameActions.ContextActionApplyBuff(applyBuff =>
                                            {
                                                applyBuff.m_Buff = sickened.ToReference<BlueprintBuffReference>();
                                                applyBuff.DurationValue.Rate = DurationRate.Minutes;
                                                applyBuff.DurationValue.BonusValue = 1;
                                            }))));
                                }));
                        }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("SickeningBlastAbility"))
                .Combine(essenceBuff)
                .Map(bps =>
                {
                    var (ability, essenceBuff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_SickeningBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Least_SickeningBlast_Description;
                    ability.m_Icon = Sprites.SickeningBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    //return (ability, new EldritchBlastComponents.EssenceEffect(essenceBuff, () => new[] { onHit() }, 2));
                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("SickeningBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_SickeningBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Least_SickeningBlast_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
