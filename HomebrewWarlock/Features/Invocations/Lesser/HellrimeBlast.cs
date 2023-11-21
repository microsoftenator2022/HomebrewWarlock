using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class HellrimeBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Hellrime Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 4" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a hellrime blast. A " +
            "hellrime blast deals cold damage. Any creature struck by the attack must make a Fortitude save or " +
            "take a –4 penalty to Dexterity for 10 minutes. The Dexterity penalties from multiple hellrime blasts " +
            "do not stack.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context)
        {

            var dexPenaltyBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("HellrimeBlastBuff"))
                .Map((BlueprintBuff buff) =>
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

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("HellrimeBlastEssenceBuff"))
                .Combine(dexPenaltyBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.PolarRay00))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.ColdCone30Feet00))
                .Map(bps =>
                {
                    (BlueprintBuff buff, var dexPenaltyBuff, var simpleProjectile, var coneProjectile) = bps.Expand();

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<EldritchBlastElementalEssence>(c =>
                    {
                        c.EquivalentSpellLevel = 4;

                        c.BlastDamageType = DamageEnergyType.Cold;

                        c.Actions.Add(GameActions.ContextActionSavingThrow(savingThrow =>
                        {
                            savingThrow.Type = SavingThrowType.Fortitude;
                            savingThrow.Actions.Add(GameActions.ContextActionConditionalSaved(save => save.Failed.Add(
                                GameActions.ContextActionApplyBuff(ab =>
                                {
                                    ab.m_Buff = dexPenaltyBuff.ToReference();
                                    ab.DurationValue.Rate = DurationRate.Minutes;
                                    ab.DurationValue.BonusValue = 10;
                                }))));
                        }));

                        c.Projectiles.Add(AbilityProjectileType.Simple, new[] { simpleProjectile.ToReference() });
                        c.Projectiles.Add(AbilityProjectileType.Cone, new[] { coneProjectile.ToReference() });
                    });

                    return buff;
                });


            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("HellrimeBlastToggleAbility"))
                .Combine(essenceBuff)
                .Combine(dexPenaltyBuff)
                //.Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.PolarRay00))
                .Map(bps =>
                {
                    var (ability, essenceBuff, dexPenaltyBuff) = bps.Expand();

                    dexPenaltyBuff.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Lesser_HellrimeBlast_DisplayName;

                    dexPenaltyBuff.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Lesser_HellrimeBlast_Description;

                    dexPenaltyBuff.m_Icon = ability.m_Icon = Sprites.HellrimeBlast;

                    ability.m_Buff = essenceBuff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("HellrimeBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
