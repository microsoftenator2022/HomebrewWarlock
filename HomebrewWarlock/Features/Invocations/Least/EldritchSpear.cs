using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;
using HomebrewWarlock.NewComponents;
using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    using BaseBlastFeatures =
        (BlueprintFeature baseFeature,
        BlueprintFeature rankFeature,
        BlueprintAbility baseAbility,
        BlueprintProjectile projectile);

    internal static class EldritchSpear
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Spear";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Eldritch Blast Shape</b>" +
            Environment.NewLine +
            "<b>Equivalent spell level:</b> 2" +
            Environment.NewLine +
            "This blast shape invocation extends your eldritch blast attacks to great distances. Eldritch spear " +
            "increases the range of an eldritch blast attack to long range.";

        public static readonly IMicroBlueprint<BlueprintAbility> AbilityRef = GeneratedGuid.EldritchSpearAbility.ToMicroBlueprint<BlueprintAbility>();

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateBlast(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var ability = baseFeatures
                .Map(bps =>
                {
                    var ability = AssetUtils.CloneBlueprint(
                        bps.baseAbility,
                        GeneratedGuid.EldritchSpearAbility);

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Least_EldritchSpear_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Least_EldritchSpear_Description;

                    ability.m_Icon = Sprites.EldritchSpear;

                    ability.GetComponent<EldritchBlastCalculateSpellLevel>().BaseEquivalentSpellLevel = 2;

                    ability.Range = AbilityRange.Long;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.EldritchSpearFeature)
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
