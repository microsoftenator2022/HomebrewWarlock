using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.EldritchBlast
{
    using BaseBlastFeatures =
        (BlueprintFeature blastFeature,
        BlueprintFeature prerequisite,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

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

        public static readonly IMicroBlueprint<BlueprintFeature> FeatureRef = GeneratedGuid.EldritchBlastRank.ToMicroBlueprint<BlueprintFeature>();
        public static readonly IMicroBlueprint<BlueprintFeature> RankFeatureRef = GeneratedGuid.EldritchBlastRank.ToMicroBlueprint<BlueprintFeature>();
        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.EldritchBlastAbility.ToMicroBlueprint<BlueprintAbility>();
        public static readonly IMicroBlueprint<BlueprintAbility> TouchAbilityRef = GeneratedGuid.EldritchBlastAbility.ToMicroBlueprint<BlueprintAbility>();

        private static readonly BlastAbility BasicBlast = new(1);

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintAbility> CreateAbility(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> projectile)
        {
            var ability = context.NewBlueprint<BlueprintAbility>(GeneratedGuid.EldritchBlastAbility)
                .Combine(projectile)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.RayItem))
                .Map(bps =>
                {
                    var (ability, projectile, rayItem) = bps.Expand();

                    ability = BasicBlast.ConfigureAbility(ability, RankFeatureRef.ToReference());
                    
                    ability.Range = AbilityRange.Close;
                    
                    ability.AddComponent<DeliverEldritchBlastProjectile>(c =>
                    {
                        c.DefaultProjectile = projectile.ToReference();

                        c.m_Length = new();
                        c.m_LineWidth = new(5);

                        c.NeedAttackRoll = true;

                        c.m_Weapon = rayItem.ToReference();
                    });

                    return ability;
                });

            return ability;
        }

        internal static BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures>
            CreateEldritchBlast(BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> projectile)
        {
            var ability = CreateAbility(context, projectile);

            var rankFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastRank"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_EldritchBlast_ShortDescription;

                    feature.m_Icon = Sprites.EldritchBlast;

                    feature.Ranks = 9;

                    return feature;
                });

            var prerequisiteFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchBlastPrerequisiteFeature"))
                .Map(feature =>
                {
                    feature.HideInUI = true;

                    return feature;
                });

            var features = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchBlastFeature"))
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_EldritchBlast_EldritchBlast_Description;
                    feature.m_DescriptionShort = LocalizedStrings.Features_EldritchBlast_EldritchBlast_ShortDescription;

                    feature.m_Icon = Sprites.EldritchBlast;
                    
                    return feature;
                })
                .Combine(prerequisiteFeature)
                .Combine(rankFeature)
                .Combine(ability)
                .Combine(projectile)
                .Map(bps =>
                {
                    var (feature, prerequisite, rankFeature, ability, _) = bps.Expand();

                    ability.m_DisplayName = feature.m_DisplayName;
                    ability.m_Description = feature.m_Description;

                    ability.m_Icon = feature.m_Icon;

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = new[]
                        {
                            ability.ToReference<BlueprintUnitFactReference>(),
                            prerequisite.ToReference<BlueprintUnitFactReference>()
                            //rankFeature.ToReference<BlueprintUnitFactReference>(),
                        };

                    });

                    feature.HideInCharacterSheetAndLevelUp = true;
                    //feature.HideInUI = true;

                    return bps.Expand();
                });

            return features;
        }

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintAbility> CreateTouchAbility(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var ability = context.NewBlueprint<BlueprintAbility>(
                GeneratedGuid.Get("EldritchBlastTouchAbility"))
                .Combine(baseFeatures)
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemWeapon.TouchItem))
                .Map(bps =>
                {
                    (BlueprintAbility ability, BaseBlastFeatures baseFeatures, var touch) = bps.Expand();

                    ability.m_DisplayName = baseFeatures.blastFeature.m_DisplayName;
                    ability.m_Description = baseFeatures.blastFeature.m_Description;

                    ability = new EldritchBlastTouch(touch.ToReference())
                        .ConfigureAbility(ability, baseFeatures.rankFeature.ToReference());

                    ability.GetComponent<AbilityEffectRunAction>().Actions.Add(new EldritchBlastOnHitFX()
                    {
                        DefaultProjectile = baseFeatures.projectile.ToReference()
                    });

                    ability.Type = AbilityType.Special;

                    return ability;
                });

            return ability;
        }
    }
}
