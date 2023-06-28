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
    internal static class EldritchSpear
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Spear";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<BlueprintProjectile> projectile)
        {
            var ability = EldritchBlast.RangedBlastTemplate(
                context,
                GeneratedGuid.Get("EldritchSpearAbility"),
                nameof(GeneratedGuid.EldritchSpearAbility),
                projectile,
                2)
                .Map(ability =>
                {
                    ability.m_DisplayName = LocalizedStrings.Features_EldritchBlast_EldritchSpear_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    ability.m_Icon = AssetUtils.GetSpriteAssemblyResource(
                        Assembly.GetExecutingAssembly(), $"{nameof(HomebrewWarlock)}.Resources.es_icon.png");

                    ability.Range = AbilityRange.Long;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("EldritchSpearFeature"),
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
