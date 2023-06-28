using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Conditions;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features
{
    internal static class FiendishResilience
    {
        [LocalizedString]
        internal const string DisplayName = "Fiendish Resilience";

        [LocalizedString]
        internal static readonly string Description =
            "Beginning at 8th level, a warlock knows the trick of fiendish resilience. Once per day, as a free " +
            "action, he can enter a state that lasts for 2 minutes. While in this state, the warlock gains fast " +
            "healing 1." + Environment.NewLine +
            "At 13th level, a warlock's fiendish resilience improves. When in his fiendish resilience state, he gains " +
            "fast healing 2 instead. At 18th level, a warlock's fiendish resilience improves to fast healing 5.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var resource = context.NewBlueprint<BlueprintAbilityResource>(
                GeneratedGuid.Get("FiendishResilienceResource"),
                nameof(GeneratedGuid.FiendishResilienceResource))
                .Map((BlueprintAbilityResource resource) => 
                {
                    resource.m_MaxAmount = new() { BaseValue = 1 };

                    return resource;
                });

            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("FiendishResiliendAbility"),
                nameof(GeneratedGuid.FiendishResiliendAbility))
                .Combine(resource)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.FastHealing1))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.FastHealing2))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.FastHealing5))
                .Map(arb =>
                {
                    var (ability, resource, buff1, buff2, buff5) = arb.Expand();

                    ability.AddAbilityResourceLogic(c =>
                    {
                        c.m_RequiredResource = resource.ToReference<BlueprintAbilityResourceReference>();
                        c.m_IsSpendResource = true;
                        c.Amount = 1;
                    });

                    ability.Type = AbilityType.Supernatural;
                    ability.Range = AbilityRange.Personal;
                    ability.CanTargetSelf = true;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Self;
                    ability.ActionType = UnitCommand.CommandType.Free;

                    ability.AddComponent<AbilityEffectRunAction>(c =>
                    {
                        c.Actions.Add(GameActions.Conditional(conditional =>
                        {
                            conditional.ConditionsChecker.Add(Conditions.ContextConditionCompare(compare =>
                            {
                                compare.m_Type = ContextConditionCompare.Type.Equal;
                                compare.CheckValue.ValueType = ContextValueType.Rank;
                                compare.TargetValue.Value = 1;
                            }));

                            conditional.IfTrue.Add(GameActions.ContextActionApplyBuff(a =>
                            {
                                a.m_Buff = buff1.ToReference<BlueprintBuffReference>();
                                a.DurationValue.Rate = DurationRate.Minutes;
                                a.DurationValue.BonusValue.Value = 2;
                            }));
                        }));

                        c.Actions.Add(GameActions.Conditional(conditional =>
                        {
                            conditional.ConditionsChecker.Add(Conditions.ContextConditionCompare(compare =>
                            {
                                compare.m_Type = ContextConditionCompare.Type.Equal;
                                compare.CheckValue.ValueType = ContextValueType.Rank;
                                compare.TargetValue.Value = 2;
                            }));

                            conditional.IfTrue.Add(GameActions.ContextActionApplyBuff(a =>
                            {
                                a.m_Buff = buff2.ToReference<BlueprintBuffReference>();
                                a.DurationValue.Rate = DurationRate.Minutes;
                                a.DurationValue.BonusValue.Value = 2;
                            }));
                        }));

                        c.Actions.Add(GameActions.Conditional(conditional =>
                        {
                            conditional.ConditionsChecker.Add(Conditions.ContextConditionCompare(compare =>
                            {
                                compare.m_Type = ContextConditionCompare.Type.Equal;
                                compare.CheckValue.ValueType = ContextValueType.Rank;
                                compare.TargetValue.Value = 3;
                            }));

                            conditional.IfTrue.Add(GameActions.ContextActionApplyBuff(a =>
                            {
                                a.m_Buff = buff5.ToReference<BlueprintBuffReference>();
                                a.DurationValue.Rate = DurationRate.Minutes;
                                a.DurationValue.BonusValue.Value = 2;
                            }));
                        }));
                    });

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get($"FiendishResilienceFeature"),
                nameof(GeneratedGuid.FiendishResilienceFeature))
                .Combine(ability)
                .Combine(resource)
                .Map(far =>
                {
                    var (feature, ability, resource) = far.Expand();

                    feature.m_DisplayName =
                    ability.m_DisplayName =
                        LocalizedStrings.Features_FiendishResilience_DisplayName;

                    feature.m_Description =
                    ability.m_Description =
                        LocalizedStrings.Features_FiendishResilience_Description;

                    feature.m_Icon =
                    ability.m_Icon =
                        AssetUtils.Direct.GetSprite("6803e805ca874084395c30178cb6eb10", 21300000);

                    feature.Ranks = 3;

                    ability.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                        c.m_Feature = feature.ToReference<BlueprintFeatureReference>();
                        c.m_Progression = ContextRankProgression.AsIs;
                        c.m_StartLevel = 0;
                        c.m_StepLevel = 1;
                    });

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = ability.ToReference<BlueprintUnitFactReference>();
                        c.m_Feature = ability.ToReference<BlueprintUnitFactReference>();

                        c.Not = true;
                    });

                    feature.AddAddAbilityResources(c =>
                    {
                        c.m_Resource = resource.ToReference<BlueprintAbilityResourceReference>();
                        c.RestoreAmount = true;
                    });

                    return feature;
                });

            return feature;
        }
    }
}
