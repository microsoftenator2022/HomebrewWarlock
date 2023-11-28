using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using HomebrewWarlock.NewActions;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

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

                foreach (var damage in damageBundle)
                {   
                    if (damage is EnergyDamage ed && ed.EnergyType == base.BlastDamageType)
                    {
                        var newDamage = new EnergyDamage(ed.Dice, ed.Bonus, ed.EnergyType);
                        newDamage.CopyFrom(ed);

                        newDamage.AddDecline(new(DamageDeclineType.Total, base.Fact));

                        newDamage.MaximumValue = 0;

                        evt.Add(newDamage);
                    }
                    else evt.Add(damage);
                }
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Utterdark Blast";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Essence</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 8" +
            Environment.NewLine +
            "This eldritch essence invocation allows you to change your eldritch blast into an utterdark blast. " +
            //"An utterdark blast deals negative energy damage, which heals undead creatures instead of damaging " +
            //"them (like inflict spells). " +
            "Any living creature struck by the attack must make a Fortitude save or gain two " +
            "negative levels. The negative levels fade after 1 hour.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var energyDrain = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.Get("UtterdarkBlastNegativeLevelAbility"))
                .Map(ability =>
                {
                    ability.Hidden = true;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_UtterdarkBlast_DisplayName;

                    ability.AddComponent<AbilityEffectRunAction>(runAction =>
                    {
                        runAction.Actions.Add(GameActions.ContextActionDealDamage(dd =>
                        {
                            dd.m_Type = ContextActionDealDamage.Type.EnergyDrain;
                            dd.EnergyDrainType = EnergyDrainType.Temporary;

                            dd.Value.BonusValue = 2;

                            dd.Duration.Rate = DurationRate.Hours;
                            dd.Duration.BonusValue = 1;
                        }));

                        ability.CanTargetEnemies = true;
                        ability.CanTargetFriends = true;
                        ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                        ability.EffectOnAlly = AbilityEffectOnUnit.Harmful;
                    });

                    return ability;
                });

            var healUndead = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.Get("UtterdarkBlastHealUndeadAbility"))
                .Map(ability =>
                {
                    ability.Hidden = true;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_UtterdarkBlast_DisplayName;

                    ability.AddContextRankConfig(c =>
                    {
                        c.m_Type = AbilityRankType.DamageDice;
                        c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                        c.m_Feature = EldritchBlast.EldritchBlast.RankFeatureRef.ToReference();
                        c.m_Progression = ContextRankProgression.AsIs;
                        c.m_StartLevel = 0;
                        c.m_StepLevel = 1;
                    });

                    ability.AddComponent<AbilityEffectRunAction>(runAction =>
                    {
                        runAction.Actions.Add(GameActions.ContextActionHealTarget(a =>
                        {
                            a.Value.DiceType = DiceType.D6;

                            a.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                            a.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
                        }),
                        GameActions.ContextActionSpawnFx(casf => casf.PrefabLink = new() { AssetId = "9a38d742801be084d89bd34318c600e8" }));
                    });

                    

                    ability.CanTargetEnemies = true;
                    ability.CanTargetFriends = true;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Helpful;
                    ability.EffectOnAlly = AbilityEffectOnUnit.Helpful;

                    return ability;
                });

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("UtterdarkBlastEssenceBuff"))
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.NegativeEnergyAffinity)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.DeathDomainGreaterLiving)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintCharacterClass.UndeadClass)
                .GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.TemporaryNegativeLevel)
                .Combine(energyDrain)
                .Combine(healUndead)
                .Map(bps =>
                {
                    (BlueprintBuff buff,
                    var negativeEnergyAffinity,
                    var deathDomainGreaterLiving,
                    var undeadClass,
                    var negativeLevel,
                    var energyDrain,
                    var healUndead) = bps.Expand();

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
                            }), Conditions.UnitClass(uc =>
                            {
                                uc.Unit = new AbilityTargetUnit();
                                uc.MinLevel = new IntConstant() { Value = 1 };
                                uc.m_Class = undeadClass.ToReference();
                                uc.Not = true;
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
                                    s.Failed.Add(GameActions.ContextActionCastSpell(cacs =>
                                    {
                                        cacs.MarkAsChild = true;
                                        cacs.m_Spell = energyDrain.ToReference();
                                    }));
                                }));
                            }));

                            conditional.IfFalse.Add(
                                new CastSpellWithContextParams()
                                {
                                    MarkAsChild = true,
                                    Spell = healUndead.ToReference()
                                });
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

                    ability.m_Buff = buff.ToReference();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    ability.DeactivateImmediately = true;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("UtterdarkBlastFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    (BlueprintFeature feature, var ability) = bps;

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
