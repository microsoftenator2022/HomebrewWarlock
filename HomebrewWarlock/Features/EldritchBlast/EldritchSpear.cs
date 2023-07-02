using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.EldritchBlast
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
        internal const string Description =
            "This blast shape invocation extends your eldritch blast attacks to great distances. Eldritch spear " +
            "increases the range of an eldritch blast attack to long range.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> CreateBlast(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BaseBlastFeatures> baseFeatures)
        {
            var ability = baseFeatures
                .Map(bps =>
                {
                    var ability = AssetUtils.CloneBlueprint(
                        bps.baseAbility,
                        GeneratedGuid.EldritchSpearAbility,
                        nameof(GeneratedGuid.EldritchSpearAbility));

                    ability.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchSpear_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_EldritchBlast_EldritchSpear_Description;

                    ability.m_Icon = AssetUtils.GetSpriteAssemblyResource(
                        Assembly.GetExecutingAssembly(), $"{nameof(HomebrewWarlock)}.Resources.es_icon.png");

                    ability.Range = AbilityRange.Long;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.EldritchSpearFeature,
                nameof(GeneratedGuid.EldritchSpearFeature))
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
