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
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class NoxiousBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Noxious Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 6" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a noxious blast. Any " +
            "creature struck by a noxious blast must make a Fortitude save or be nauseated for 1 minute.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("NoxiousBlastEssenceBuff"))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.Nauseated))
                .Map(bps =>
                {
                    var (buff, nauseated) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 6;

                        c.Actions.Add(GameActions.ContextActionSavingThrow(savingThrow =>
                            {
                                savingThrow.Type = SavingThrowType.Fortitude;
                                savingThrow.Actions.Add(
                                    GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                        GameActions.ContextActionApplyBuff(applyBuff =>
                                        {
                                            applyBuff.m_Buff = nauseated.ToReference();
                                            applyBuff.DurationValue.Rate = DurationRate.Minutes;
                                            applyBuff.DurationValue.BonusValue = 1;
                                        }))));
                            }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("NoxiousBlastToggleAbility"))
                .Combine(buff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_NoxiousBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_NoxiousBlast_Description;
                    ability.m_Icon = Sprites.NoxiousBlast;

                    ability.m_Buff = buff.ToReference();
                    
                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("NoxiousBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    var prerequisite = feature.AddPrerequisiteFeature(
                        GeneratedGuid.EldritchBlastPrerequisiteFeature.ToMicroBlueprint<BlueprintFeature>());

                    prerequisite.HideInUI = true;

                    return feature;
                });

            return feature;
        }
    }
}
