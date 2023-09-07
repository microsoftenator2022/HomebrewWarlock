using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class VitriolicBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Vitriolic Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 6" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into a vitriolic blast. A " +
            "vitriolic blast deals acid damage, and it is formed from conjured acid, making it different from " +
            "other eldritch essences because it ignores spell resistance. Creatures struck by a vitriolic blast " +
            "automatically take an extra 2d6 points of acid damage on following rounds. This acid damage persists " +
            "for 1 round per five class levels you have." +
            Environment.NewLine +
            "For example, a 15th-level warlock deals 2d6 points of acid damage per round for 3 rounds after the " +
            "initial vitriolic blast attack.";

        [TypeId("d2585409-6365-465f-8de8-d6bfc795a5fe")]
        internal class Essence : EldritchBlastElementalEssence, IInitiatorRulebookHandler<RuleSpellResistanceCheck>
        {
            public void OnEventAboutToTrigger(RuleSpellResistanceCheck evt)
            {
                if (!evt.Ability.ComponentsArray.OfType<EldritchBlastCalculateSpellLevel>().Any())
                    return;

                evt.IgnoreSpellResistance = true;
            }
            public void OnEventDidTrigger(RuleSpellResistanceCheck evt) { }
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var dotBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("VitriolicBlastPerRoundDamage"))
                .Map((BlueprintBuff buff) =>
                {
                    buff.AddComponent<AddFactContextActions>(c =>
                    {
                        c.NewRound.Add(GameActions.ContextActionDealDamage(a =>
                        {
                            a.DamageType.Type = DamageType.Energy;
                            a.DamageType.Energy = DamageEnergyType.Acid;
                            a.Value.DiceType = DiceType.D6;
                            a.Value.DiceCountValue = 2;
                        }));
                    });

                    buff.m_Flags = BlueprintBuff.Flags.Harmful;

                    buff.Stacking = StackingType.Prolong;

                    return buff;
                });

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("VitriolicBlastEssenceBuff"))
                .Combine(dotBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.GiantSlugAcid00))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.AcidCone30Feet00))
                .Map(bps =>
                {
                    (BlueprintBuff buff, var dotBuff, var simpleProjectile, var coneProjectile) = bps.Expand();

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<Essence>(c =>
                    {
                        c.EquivalentSpellLevel = 6;

                        c.BlastDamageType = DamageEnergyType.Acid;
                        
                        c.Actions.Add(GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.m_Buff = dotBuff.ToReference();
                            ab.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
                        }));

                        c.Projectiles.Add(AbilityProjectileType.Simple, new[] { simpleProjectile.ToReference() });
                        c.Projectiles.Add(AbilityProjectileType.Cone, new[] { coneProjectile.ToReference() });
                    });

                    buff.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                        c.m_Class = new[]
                        {
                            WarlockClass.Blueprint.ToReference()
                        };
                        c.m_Progression = ContextRankProgression.DelayedStartPlusDivStep;
                        c.m_StepLevel = 5;
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("VitriolicBlastToggleAbility"))
                .Combine(essenceBuff)
                .Combine(dotBuff)
                .Map(bps =>
                {
                    var (ability, essenceBuff, dotBuff) = bps.Expand();

                    dotBuff.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Greater_VitriolicBlast_DisplayName;
                    dotBuff.m_Description = ability.m_Description =
                        LocalizedStrings.Features_Invocations_Greater_VitriolicBlast_Description;

                    dotBuff.m_Icon = ability.m_Icon = Sprites.VitriolicBlast;

                    ability.m_Buff = essenceBuff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("VitriolicBlastFeature"))
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
