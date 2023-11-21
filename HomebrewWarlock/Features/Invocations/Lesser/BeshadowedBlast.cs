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

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class BeshadowedBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Beshadowed Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 4" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a beshadowed blast. Any " +
            "creature struck by a beshadowed blast must succeed on a Fortitude save or be blinded for 1 round.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BeshadowedBlastEssenceBuff"))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.BlindnessBuff))
                .Map(bps =>
                {
                    var (buff, blindness) = bps;

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 4;

                        c.Actions.Add(GameActions.ContextActionSavingThrow(savingThrow =>
                        {
                            savingThrow.Type = SavingThrowType.Fortitude;
                            savingThrow.Actions.Add(GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                GameActions.ContextActionApplyBuff(ab =>
                                {
                                    ab.m_Buff = blindness.ToReference();
                                    ab.DurationValue.BonusValue = 1;
                                }))));
                        }));
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("BeshadowedBlastToggleAbility"))
                .Combine(essenceBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.BlindnessBuff))
                .Map(bps =>
                {
                    var (ability, essenceBuff, blindness) = bps.Expand();

                    ability.m_Buff = essenceBuff.ToReference();
                    
                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;
                    
                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("BeshadowedBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_BeshadowedBlast_DisplayName;

                    feature.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Lesser_BeshadowedBlast_Description;

                    feature.m_Icon = ability.m_Icon = Sprites.BeshadowedBlast;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
