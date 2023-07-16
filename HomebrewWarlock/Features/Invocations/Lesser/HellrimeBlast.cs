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
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class HellrimeBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Hellrime Blast";

        [LocalizedString]
        internal const string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into a hellrime blast. A " +
            "hellrime blast deals cold damage. Any creature struck by the attack must make a Fortitude save or " +
            "take a –4 penalty to Dexterity for 10 minutes. The Dexterity penalties from multiple hellrime blasts " +
            "do not stack.";

        internal static BlueprintInitializationContext.ContextInitializer<EssenceFeature> Create(
            BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("HellrimeBlastEssenceBuff"),
                nameof(GeneratedGuid.HellrimeBlastEssenceBuff))
                .Map(buff =>
                {
                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("HellrimeBlastBuff"),
                nameof(GeneratedGuid.HellrimeBlastBuff))
                .Map(buff =>
                {
                    buff.AddAddStatBonus(c =>
                    {
                        c.Descriptor = ModifierDescriptor.NegativeEnergyPenalty;
                        c.Stat = StatType.Dexterity;
                        c.Value = -4;
                    });

                    buff.m_Flags = BlueprintBuff.Flags.Harmful;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("HellrimeBlastToggleAbility"),
                nameof(GeneratedGuid.HellrimeBlastToggleAbility))
                .Combine(essenceBuff)
                .Combine(buff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.PolarRay00))
                .Map(bps =>
                {
                    var (ability, essenceBuff, dexPenaltyBuff, projectile) = bps.Expand();

                    dexPenaltyBuff.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_HellrimeBlast_DisplayName;

                    dexPenaltyBuff.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Lesser_HellrimeBlast_Description;

                    dexPenaltyBuff.m_Icon = ability.m_Icon = Sprites.HellrimeBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    GameAction onHit() => GameActions.ContextActionSavingThrow(savingThrow =>
                    {
                        savingThrow.Type = SavingThrowType.Fortitude;
                        savingThrow.Actions.Add(GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                            GameActions.ContextActionApplyBuff(ab =>
                            {
                                ab.m_Buff = dexPenaltyBuff.ToReference<BlueprintBuffReference>();
                                ab.DurationValue.Rate = DurationRate.Minutes;
                                ab.DurationValue.BonusValue = 10;
                            }))));
                    });

                    return (ability, new EldritchBlastComponents.EssenceEffect(
                        essenceBuff,
                        () => new[] { onHit() },
                        4,
                        DamageEnergyType.Cold,
                        new[]
                        {
                            (AbilityProjectileType.Simple,
                            new[] { projectile.ToReference<BlueprintProjectileReference>() })
                        }));
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("HellrimeBlastFeature"),
                nameof(GeneratedGuid.HellrimeBlastFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability, essenceEffect) = bps.Flatten();

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return new EssenceFeature(feature, essenceEffect);
                });

            return feature;
        }
    }
}
