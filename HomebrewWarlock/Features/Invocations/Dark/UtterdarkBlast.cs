using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast.Components;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Dark
{
    internal static class UtterdarkBlast
    {
        internal class Essence : EldritchBlastElementalEssence
        {
            public Essence()
            {
                base.BlastDamageType = DamageEnergyType.NegativeEnergy;
            }

            public ConditionsChecker DamageConditions = new();

            public override void OnEventAboutToTrigger(RuleDealDamage evt)
            {
                base.OnEventAboutToTrigger(evt);

                if (DamageConditions.Check())
                    return;

                var damageBundle = evt.DamageBundle.ToArray();
                evt.Remove(_ => true);

                foreach (var damage in damageBundle.Where(damage =>
                    damage is not EnergyDamage ed || ed.EnergyType != base.BlastDamageType))
                {   
                    evt.Add(damage);
                }
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Utterdark Blast";

        [LocalizedString]
        internal const string Description =
            "This eldritch essence invocation allows you to change your eldritch blast into an utterdark blast. " +
            "An utterdark blast deals negative energy damage, which heals undead creatures instead of damaging " +
            "them (like inflict spells). Any creature struck by the attack must make a Fortitude save or gain two " +
            "negative levels. The negative levels fade after 1 hour.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var essenceBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("UtterdarkBlastEssenceBuff"))
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.NegativeEnergyAffinity)
                .Combine(BlueprintsDb.Owlcat.BlueprintFeature.DeathDomainGreaterLiving)
                .Combine(BlueprintsDb.Owlcat.BlueprintBuff.TemporaryNegativeLevel)
                .Map(bps =>
                {
                    var (buff, negativeEnergyAffinity, deathDomainGreaterLiving, negativeLevel) = bps.Expand();

                    buff.AddComponent<Essence>(c =>
                    {
                        c.EquivalentSpellLevel = 8;

                        c.DamageConditions.Add(
                            Conditions.ContextConditionHasFact(condition =>
                            {
                                condition.m_Fact = negativeEnergyAffinity.ToReference<BlueprintUnitFactReference>();
                                condition.Not = true;
                            }),
                            Conditions.ContextConditionHasFact(condition =>
                            {
                                condition.m_Fact = deathDomainGreaterLiving.ToReference<BlueprintUnitFactReference>();
                                condition.Not = true;
                            }));

                        c.DamageConditions.Operation = Operation.And;

                        c.Actions.Add(GameActions.Conditional(conditional =>
                        {
                            conditional.ConditionsChecker = c.DamageConditions;

                            conditional.IfTrue.Add(GameActions.ContextActionSavingThrow(st =>
                            {
                                st.Type = SavingThrowType.Fortitude;
                                st.Actions.Add(GameActions.ContextActionConditionalSaved(s =>
                                {
                                    s.Failed.Add(
                                        GameActions.ContextActionApplyBuff(ab =>
                                        {
                                            ab.m_Buff = negativeLevel.ToReference<BlueprintBuffReference>();
                                            ab.IsNotDispelable = true;

                                            ab.DurationValue.BonusValue = 1;
                                            ab.DurationValue.Rate = DurationRate.Hours;
                                        }),
                                        GameActions.ContextActionApplyBuff(ab =>
                                        {
                                            ab.m_Buff = negativeLevel.ToReference<BlueprintBuffReference>();
                                            ab.IsNotDispelable = true;

                                            ab.DurationValue.BonusValue = 1;
                                            ab.DurationValue.Rate = DurationRate.Hours;
                                        }));
                                }));
                            }));

                            conditional.IfFalse.Add(GameActions.ContextActionHealTarget(a =>
                            {
                                a.Value.DiceType = DiceType.D6;
                                
                                a.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                                a.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
                            }));
                        }));
                    });

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(GeneratedGuid.Get("UtterdarkBlastToggleAbility"))
                .Combine(essenceBuff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_UtterdarkBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Dark_UtterdarkBlast_Description;
                    ability.m_Icon = Sprites.UtterdarkBlast;

                    ability.m_Buff = buff.ToReference<BlueprintBuffReference>();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("UtterdarkBlastFeature"))
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
