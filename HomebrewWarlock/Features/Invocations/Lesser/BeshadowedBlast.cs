using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Components;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class BeshadowedBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Beshadowed Blast";

        [LocalizedString]
        internal const string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into a beshadowed blast. Any " +
            "creature struck by a beshadowed blast must succeed on a Fortitude save or be blinded for 1 round.";

        internal static BlueprintInitializationContext.ContextInitializer<EssenceFeature> Create(BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("BeshadowedBlastEssenceBuff"),
                nameof(GeneratedGuid.BeshadowedBlastEssenceBuff))
                .Map(buff =>
                {
                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("BeshadowedBlastToggleAbility"),
                nameof(GeneratedGuid.BeshadowedBlastToggleAbility))
                .Combine(essenceBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.BlindnessBuff))
                .Map(bps =>
                {
                    var (ability, essenceBuff, blindness) = bps.Expand();

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();
                    ability.Group = Invocation.EssenceInvocationAbilityGroup;

                    ContextActionSavingThrow onHit() => GameActions.ContextActionSavingThrow(savingThrow =>
                    {
                        savingThrow.Type = SavingThrowType.Fortitude;
                        savingThrow.Actions.Add(GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                            GameActions.ContextActionApplyBuff(ab =>
                            {
                                ab.m_Buff = blindness.ToReference<BlueprintBuffReference>();
                                ab.DurationValue.BonusValue = 1;
                            }))));
                    });

                    return (ability,
                        new EldritchBlastComponents.EssenceEffect(
                        essenceBuff,
                        () => new[] { onHit() },
                        4));
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("BeshadowedBlastFeature"),
                nameof(GeneratedGuid.BeshadowedBlastFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability, essenceEffect) = bps.Flatten();

                    feature.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_BeshadowedBlast_DisplayName;

                    feature.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Lesser_BeshadowedBlast_Description;

                    feature.m_Icon = ability.m_Icon = Sprites.BeshadowedBlast;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return new EssenceFeature(feature, essenceEffect);
                });

            return feature;
        }
    }
}
