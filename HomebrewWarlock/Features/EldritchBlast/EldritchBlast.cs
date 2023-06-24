using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.GameActions;
using MicroWrath.Localization;
using MicroWrath.Util;

using UnityEngine;


namespace HomebrewWarlock.Features
{
    public static partial class EldritchBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Blast";

        [LocalizedString]
        internal static readonly string Description =
            "The first ability a warlock learns is eldritch blast. A warlock attacks his foes " +
            "with eldritch power, using baleful magical energy to deal damage and sometimes impart other " +
            $"debilitating effects.{Environment.NewLine}" +
            "An eldritch blast is a ray with a range of 60 feet. It is a ranged touch attack that affects a " +
            "single target, allowing no saving throw. An eldritch blast deals 1d6 points of damage at 1st level " +
            "and increases in power as the warlock rises in level. An eldritch blast is the equivalent of a " +
            "1st-level spell. If you apply a blast shape or eldritch essence invocation to your eldritch blast, " +
            $"your eldritch blast uses the level equivalent of the shape or essence.{Environment.NewLine}" +
            "An eldritch blast is subject to spell resistance, although the Spell Penetration feat and other " +
            "effects that improve caster level checks to overcome spell resistance also apply to eldritch " +
            "blast. An eldritch blast deals half damage to objects. Metamagic feats cannot improve a warlock's " +
            "eldritch blast (because it is a spell-like ability, not a spell). However, the feat Ability Focus " +
            "(eldritch blast) increases the DC for all saving throws (if any) associated with a warlock's " +
            "eldritch blast by 2.";

        [LocalizedString]
        internal const string ShortDescription =
            "A warlock attacks his foes with eldritch power, using baleful magical energy to deal damage and " +
            "sometimes impart other debilitating effects.";


        internal static readonly IMicroBlueprint<BlueprintFeature> Feature = new MicroBlueprint<BlueprintFeature>(GeneratedGuid.EldritchBlastFeature);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateFeature(BlueprintInitializationContext context)
        {
            static Sprite getSprite() => AssetUtils.Direct.GetSprite("fdfbce1816665e74584c528faebcc381", 21300000);

            var ability = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.Get("EldritchBlastAbility"), nameof(GeneratedGuid.EldritchBlastAbility))
                .Map((BlueprintAbility ability) =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_EldritchBlast_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_EldritchBlast_Description;

                    ability.m_Icon = getSprite();

                    ability.Type = AbilityType.Special;
                    ability.Range = AbilityRange.Close;

                    ability.CanTargetEnemies = true;
                    ability.SpellResistance = true;
                    ability.EffectOnAlly = AbilityEffectOnUnit.None;
                    ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
                    ability.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Directional;
                    ability.ActionType = UnitCommand.CommandType.Standard;

                    return ability;
                });

            var projectile = CreateProjectile(context);

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastFeature"), nameof(GeneratedGuid.EldritchBlastFeature))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_ShortDescription;

                    feature.m_Icon = getSprite();

                    feature.Ranks = 9;
                    
                    return feature;
                })
                .Combine(ability)
                .Combine(projectile)
                .Map(fa =>
                {
                    var (feature, ability, projectile) = fa.Flatten();

                    feature.AddAddFeatureIfHasFact(c =>
                    {
                        c.m_CheckedFact = ability.ToReference<BlueprintUnitFactReference>();
                        c.m_Feature = ability.ToReference<BlueprintUnitFactReference>();

                        c.Not = true;
                    });

                    ability.AddComponent<AbilityDeliverProjectile>(c =>
                    {
                        c.m_Projectiles = new[]
                        {
                            //BlueprintsDb.Owlcat.BlueprintProjectile.Disintegrate00.ToReference<BlueprintProjectile, BlueprintProjectileReference>()
                            projectile.ToReference<BlueprintProjectileReference>()
                        };

                        c.m_Length = new();
                        c.m_LineWidth = new() { m_Value = 5 };

                        c.NeedAttackRoll = true;

                        c.m_Weapon = BlueprintsDb.Owlcat.BlueprintItemWeapon.RayItem.ToReference<BlueprintItemWeapon, BlueprintItemWeaponReference>();
                    });

                    AddOnHitEffectToAbility(ability);

                    //ability.AddComponent<AbilityEffectRunAction>(c =>
                    //{
                    //    c.Actions.Add(
                    //        GameActions.ContextActionDealDamage(action =>
                    //        {
                    //            action.m_Type = ContextActionDealDamage.Type.Damage;
                               
                    //            action.DamageType.Type = DamageType.Energy;
                    //            action.DamageType.Energy = DamageEnergyType.Magic;

                    //            action.Value.DiceType = DiceType.D6;

                    //            action.Value.DiceCountValue.ValueType = ContextValueType.Rank;
                    //            action.Value.DiceCountValue.Value = 1;
                    //            action.Value.DiceCountValue.ValueRank = AbilityRankType.DamageDice;
                    //        }));
                    //});

                    //ability.AddContextRankConfig(c =>
                    //{
                    //    c.m_Type = AbilityRankType.DamageDice;
                    //    c.m_BaseValueType = ContextRankBaseValueType.FeatureRank;
                    //    c.m_Feature = feature.ToReference<BlueprintFeatureReference>();
                    //    c.m_Progression = ContextRankProgression.AsIs;
                    //    c.m_StartLevel = 0;
                    //    c.m_StepLevel = 1;
                    //});

                    //ability.AddComponent<ContextCalculateAbilityParams>(c =>
                    //{
                    //    c.StatType = StatType.Charisma;
                    //    c.ReplaceSpellLevel = true;
                    //    c.SpellLevel = 1;
                    //});

                    return feature;
                });

            return feature;
        }

        internal class EssenceComponent : ActivatableAbilityGroupComponent<EssenceComponent>
        {
            public int EquivalentSpellLevel = 1;
        }

        //public class DCCalc : ContextAbilityParamsCalculator
        //{
        //    public override AbilityParams Calculate(MechanicsContext context)
        //    {
        //        MicroLogger.Debug(() => $"{nameof(DCCalc)}.{nameof(Calculate)}");

        //        if (context.MaybeCaster is not UnitEntityData caster)
        //        {
        //            MicroLogger.Debug(() => "Caster is null");
        //            return context.Params;
        //        }
        //        var ability = context.SourceAbilityContext?.Ability;

        //        MicroLogger.Debug(() => $"Ability: {ability?.ToString() ?? "&lt; null &gt;"}");
        //        MicroLogger.Debug(() => $"Blueprint: {context.AssociatedBlueprint}");

        //        var ruleCalculateAbilityParams = (ability is not null) ?
        //            new RuleCalculateAbilityParams(caster, ability) :
        //            new RuleCalculateAbilityParams(caster, context.AssociatedBlueprint, null);

        //        var essence = context.MaybeCaster.ActivatableAbilities.Enumerable
        //            .FirstOrDefault(ability => ability.Components.OfType<EssenceComponent>().Any() && ability.IsOn)
        //                ?.Components
        //                ?.OfType<EssenceComponent>()
        //                ?.OrderByDescending(c => c.EquivalentSpellLevel)
        //                ?.FirstOrDefault();

        //        ruleCalculateAbilityParams.ReplaceSpellLevel = essence?.EquivalentSpellLevel ?? 1;
        //        ruleCalculateAbilityParams.ReplaceStat = StatType.Charisma;
        //        ruleCalculateAbilityParams.ReplaceCasterLevel = 0;

        //        var result = context.TriggerRule(ruleCalculateAbilityParams).Result;

        //        MicroLogger.Debug(() =>
        //        {
        //            var sb = new StringBuilder();
        //            sb.AppendLine($"Source: {result.SpellSource}");
        //            sb.AppendLine($"Is changed? {result.IsChanged}");
        //            sb.AppendLine($"Caster level: {result.CasterLevel}");
        //            sb.AppendLine($"Spell level: {result.SpellLevel}");
        //            sb.AppendLine($"Rank bonus: {result.RankBonus}");
        //            sb.AppendLine($"DC: {result.DC}");

        //            return sb.ToString();
        //        });

        //        return result;
        //    }
        //}
    }
}
