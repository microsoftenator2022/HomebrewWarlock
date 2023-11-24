using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class BewitchingBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Bewitching Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 4" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a bewitching blast. " +
            "Any creature struck by a bewitching blast must succeed on a Will save or be confused for 1 round in " +
            "addition to the normal damage from the blast.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BewitchingBlastEssenceBuff"))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Confusion))
                .Map(bps =>
                {
                    var (buff, confusion) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 4;

                        c.Actions.Add(GameActions.ContextActionSavingThrow(savingThrow =>
                        {
                            savingThrow.Type = SavingThrowType.Will;
                            savingThrow.Actions.Add(
                                GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                    GameActions.ContextActionApplyBuff(applyBuff =>
                                    {
                                        applyBuff.m_Buff = confusion.ToReference();
                                        applyBuff.DurationValue.BonusValue = 1;
                                    }))));
                        }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("BewitchingBlastEssenceToggleAbility"))
                .Combine(buff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_BewitchingBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_BewitchingBlast_Description;
                    ability.m_Icon = Sprites.BewitchingBlast;

                    ability.m_Buff = buff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("BewitchingBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    feature.AddPrerequisiteFeature(GeneratedGuid.EldritchBlastPrerequisiteFeature.ToMicroBlueprint<BlueprintFeature>());

                    return feature;
                });

            return feature;
        }
    }
}
