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
                GeneratedGuid.Get("VitriolicBlastPerRoundDamage"),
                nameof(GeneratedGuid.VitriolicBlastPerRoundDamage))
                .Map(buff =>
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

            //var applyDotBuff = context.NewBlueprint<BlueprintBuff>(
            //    GeneratedGuid.Get("VitriolicBlastApplyPerRoundDamage"),
            //    nameof(GeneratedGuid.VitriolicBlastApplyPerRoundDamage))
            //    .Combine(dotBuff)
            //    .Map(bps =>
            //    {
            //        var (dotBuff, applyBuff) = bps;

            //        applyBuff.AddComponent<AddFactContextActions>(c =>
            //        {
            //            c.Activated.Add(GameActions.ContextActionApplyBuff(ab =>
            //            {
            //                ab.m_Buff = dotBuff.ToReference<BlueprintBuffReference>();
            //                ab.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
            //                ab.AsChild = false;
            //                //ab.IsNotDispelable = true;
            //            }));
            //        });

            //        applyBuff.AddContextRankConfig(c =>
            //        {
            //            c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
            //            c.m_Class = new[]
            //            {
            //                WarlockClass.Blueprint.ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>()
            //            };
            //            c.m_Progression = ContextRankProgression.DelayedStartPlusDivStep;
            //            c.m_StepLevel = 5;
            //        });

            //        //applyBuff.Stacking = StackingType.Stack;

            //        applyBuff.m_Flags = BlueprintBuff.Flags.Harmful | BlueprintBuff.Flags.HiddenInUi;

            //        return applyBuff;
            //    });

            var essenceBuff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("VitriolicBlastEssenceBuff"),
                nameof(GeneratedGuid.VitriolicBlastEssenceBuff))
                .Combine(dotBuff)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.GiantSlugAcid00))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintProjectile.AcidCone30Feet00))
                .Map(bps =>
                {
                    var (buff, dotBuff, simpleProjectile, coneProjectile) = bps.Expand();

                    buff.m_Flags = BlueprintBuff.Flags.StayOnDeath | BlueprintBuff.Flags.HiddenInUi;

                    buff.AddComponent<Essence>(c =>
                    {
                        c.EquivalentSpellLevel = 6;

                        c.BlastDamageType = DamageEnergyType.Acid;
                        
                        c.Actions.Add(GameActions.ContextActionApplyBuff(ab =>
                        {
                            ab.m_Buff = dotBuff.ToReference<BlueprintBuffReference>();
                            ab.DurationValue.BonusValue.ValueType = ContextValueType.Rank;
                        }));

                        c.Projectiles.Add(AbilityProjectileType.Simple, new[] { simpleProjectile.ToReference<BlueprintProjectileReference>() });
                        c.Projectiles.Add(AbilityProjectileType.Cone, new[] { coneProjectile.ToReference<BlueprintProjectileReference>() });
                    });

                    buff.AddContextRankConfig(c =>
                    {
                        c.m_BaseValueType = ContextRankBaseValueType.ClassLevel;
                        c.m_Class = new[]
                        {
                            WarlockClass.Blueprint.ToReference<BlueprintCharacterClass, BlueprintCharacterClassReference>()
                        };
                        c.m_Progression = ContextRankProgression.DelayedStartPlusDivStep;
                        c.m_StepLevel = 5;
                    });

                    return buff;
                });

            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("VitriolicBlastToggleAbility"),
                nameof(GeneratedGuid.VitriolicBlastToggleAbility))
                .Combine(essenceBuff)
                .Combine(dotBuff)
                .Map(bps =>
                {
                    var (ability, essenceBuff, dotBuff) = bps.Expand();

                    dotBuff.m_DisplayName = ability.m_DisplayName =
                        LocalizedStrings.Features_Invocations_Greater_VitriolicBlast_DisplayName;

                    dotBuff.m_Icon = ability.m_Icon = Sprites.VitriolicBlast;

                    ability.m_Buff = essenceBuff.ToReference<BlueprintBuffReference>();

                    ability.Group = InvocationComponents.EssenceInvocationAbilityGroup;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("VitriolicBlastFeature"),
                nameof(GeneratedGuid.VitriolicBlastFeature))
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
